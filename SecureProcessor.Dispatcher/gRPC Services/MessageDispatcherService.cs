using Grpc.Core;
using Microsoft.Extensions.Logging;
using SecureProcessor.Core.Services;
using SecureProcessor.Dispatcher.Services;
using SecureProcessor.Shared.Models;
using SecureProcessor.Shared.Protos;
using System.Collections.Concurrent;

namespace SecureProcessor.Dispatcher.gRPC_Services
{
    /// <summary>
    /// Unified gRPC service for all dispatcher operations
    /// </summary>
    // ✅ تغییر نام کلاس برای جلوگیری از تداخل
    public class MessageDispatcherServiceImpl : MessageDispatcherService.MessageDispatcherServiceBase
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IProcessorManagerService _processorManager;
        private readonly ILogger<MessageDispatcherServiceImpl> _logger;

        // صف داخلی برای پیام‌های خارجی
        internal static readonly ConcurrentQueue<Shared.Models.ExternalMessageWrapper> ExternalMessages = new();
        private static readonly object _queueLock = new object();

        public MessageDispatcherServiceImpl(IMessageQueueService messageQueueService,
            IProcessorManagerService processorManager,
            ILogger<MessageDispatcherServiceImpl> logger)
        {
            _messageQueueService = messageQueueService;
            _processorManager = processorManager;
            _logger = logger;
        }

        /// <summary>
        /// Handles bidirectional streaming communication with processors
        /// </summary>
        public override async Task Connect(IAsyncStreamReader<ProcessorMessage> requestStream,
            IServerStreamWriter<DispatcherMessage> responseStream,
            ServerCallContext context)
        {
            _logger.LogInformation("🔌 New processor connection established");

            // Handle incoming messages from processor
            var requestHandler = Task.Run(async () =>
            {
                await foreach (var message in requestStream.ReadAllAsync())
                {
                    try
                    {
                        switch (message.ContentCase)
                        {
                            case ProcessorMessage.ContentOneofCase.Introduction:
                                await HandleIntroductionAsync(message.Introduction, responseStream);
                                break;
                            case ProcessorMessage.ContentOneofCase.Result:
                                await HandleResultAsync(message.Result);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error processing message from processor");
                    }
                }
            });

            // Wait for the connection to be closed
            await requestHandler;
            _logger.LogInformation("🔌 Processor connection closed");
        }

        /// <summary>
        /// دریافت پیام خارجی از Manager
        /// </summary>
        public override async Task<ExternalMessageResponse> SubmitExternalMessage(ExternalMessageRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"📥 EXTERNAL MESSAGE RECEIVED: {request.RequestId}");
            _logger.LogInformation($"   Message ID: {request.Message.Id}");

            try
            {
                // اضافه کردن پیام به صف داخلی
                var wrapper = new Shared.Models.ExternalMessageWrapper
                {
                    Message = new Shared.Models.Message
                    {
                        Id = request.Message.Id,
                        Sender = request.Message.Sender,
                        Content = request.Message.Content
                    },
                    RequestId = request.RequestId,
                    RequesterId = request.RequesterId,
                    ReceivedTime = DateTime.UtcNow
                };

                lock (_queueLock)
                {
                    ExternalMessages.Enqueue(wrapper);
                }

                _logger.LogInformation($"✅ EXTERNAL MESSAGE QUEUED: {request.RequestId}");

                var response = new ExternalMessageResponse
                {
                    Status = "Queued",
                    Message = "Message added to processing queue successfully",
                    ResponseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                return await Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 ERROR QUEUING EXTERNAL MESSAGE");

                return new ExternalMessageResponse
                {
                    Status = "Error",
                    Message = $"Failed to queue message: {ex.Message}",
                    ResponseTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }
        }

        /// <summary>
        /// سلامت‌سنجی از طریق gRPC
        /// </summary>
        public override async Task<Shared.Protos.HealthCheckResponse> HealthCheck(Shared.Protos.HealthCheckRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"🏥 HEALTH CHECK REQUEST: {request.Id}");
            _logger.LogInformation($"   Connected Clients: {request.NumberOfConnectedClients}");

            var response = new Shared.Protos.HealthCheckResponse
            {
                IsEnabled = true,
                NumberOfActiveClients = Math.Min(request.NumberOfConnectedClients, 5),
                ExpirationTime = DateTime.UtcNow.AddMinutes(10).ToString("o")
            };

            _logger.LogInformation($"✅ HEALTH CHECK RESPONSE: Active={response.NumberOfActiveClients}");

            return await Task.FromResult(response);
        }

        private async Task HandleIntroductionAsync(Introduction introduction,
            IServerStreamWriter<DispatcherMessage> responseStream)
        {
            _logger.LogInformation($"👋 Processor introduced: {introduction.Id} ({introduction.Type})");

            await _processorManager.RegisterProcessorAsync(introduction.Id, introduction.Type);

            // Send configuration to processor
            var configuration = new Configuration();
            configuration.Rules.Add("word_count", @"\b\w+\b");
            configuration.Rules.Add("email", @"[\w\.-]+@[\w\.-]+\.\w+");

            var configMessage = new DispatcherMessage
            {
                Config = configuration
            };

            await responseStream.WriteAsync(configMessage);
            _logger.LogInformation($"⚙️ Configuration sent to processor {introduction.Id}");
        }

        private async Task HandleResultAsync(ResultMessage result)
        {
            var additionalFields = new Dictionary<string, bool>();
            if (result.AdditionalFields?.Fields != null)
            {
                foreach (var kvp in result.AdditionalFields.Fields)
                {
                    additionalFields[kvp.Key] = kvp.Value;
                }
            }

            var processedMessage = new ProcessedMessage
            {
                Id = result.Id,
                Engine = result.Engine,
                MessageLength = result.MessageLength,
                IsValid = result.IsValid,
                AdditionalFields = additionalFields
            };

            await _messageQueueService.SendProcessedMessageAsync(processedMessage);
            _logger.LogInformation($"✅ Processed result received for message {result.Id}");
        }


        internal static int GetExternalMessageCount()
        {
            lock (_queueLock)
            {
                return ExternalMessages.Count;
            }
        }
    } 
}