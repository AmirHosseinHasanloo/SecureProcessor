using Microsoft.Extensions.Logging;
using SecureProcessor.Core.Services;
using SecureProcessor.Dispatcher.Services;
using SecureProcessor.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Dispatcher.gRPC_Services
{
    /// <summary>
    /// gRPC service for bidirectional communication with message processors
    /// </summary>
    public class MessageDispatcherService : MessageDispatcher.MessageDispatcherBase
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IProcessorManagerService _processorManager;
        private readonly ILogger<MessageDispatcherService> _logger;

        public MessageDispatcherService(IMessageQueueService messageQueueService,
            IProcessorManagerService processorManager,
            ILogger<MessageDispatcherService> logger)
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
            _logger.LogInformation("New processor connection established");

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
                        _logger.LogError(ex, "Error processing message from processor");
                    }
                }
            });

            // Wait for the connection to be closed
            await requestHandler;
            _logger.LogInformation("Processor connection closed");
        }

        private async Task HandleIntroductionAsync(Introduction introduction,
            IServerStreamWriter<DispatcherMessage> responseStream)
        {
            _logger.LogInformation($"Processor introduced: {introduction.Id} ({introduction.Type})");

            await _processorManager.RegisterProcessorAsync(introduction.Id, introduction.Type);

            // Send configuration to processor
            var configMessage = new DispatcherMessage
            {
                Config = { ["word_count"] = @"\b\w+\b", ["email"] = @"[\w\.-]+@[\w\.-]+\.\w+" }
            };

            await responseStream.WriteAsync(configMessage);
            _logger.LogInformation($"Configuration sent to processor {introduction.Id}");
        }

        private async Task HandleResultAsync(ResultMessage result)
        {
            var processedMessage = new ProcessedMessage
            {
                Id = result.Id,
                Engine = result.Engine,
                MessageLength = result.MessageLength,
                IsValid = result.IsValid,
                AdditionalFields = result.AdditionalFields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            await _messageQueueService.SendProcessedMessageAsync(processedMessage);
            _logger.LogInformation($"Processed result received for message {result.Id}");
        }
    }
}
