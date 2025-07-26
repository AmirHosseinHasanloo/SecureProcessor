using Grpc.Core;
using Microsoft.Extensions.Logging;
using SecureProcessor.Processor.Services;
using Models = SecureProcessor.Shared.Models;
using Protos = SecureProcessor.Shared.Protos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecureProcessor.Processor.gRPC_Client
{
    /// <summary>
    /// gRPC client service for communicating with dispatcher
    /// </summary>
    public class ProcessorClientService
    {
        // ✅ استفاده از نام صحیح کلاینت
        private readonly Protos.MessageDispatcherService.MessageDispatcherServiceClient _client;
        private readonly IMessageProcessorService _messageProcessor;
        private readonly ILogger<ProcessorClientService> _logger;
        private readonly string _processorId;

        // ✅ تغییر نوع پارامتر
        public ProcessorClientService(Protos.MessageDispatcherService.MessageDispatcherServiceClient client,
            IMessageProcessorService messageProcessor,
            ILogger<ProcessorClientService> logger)
        {
            _client = client;
            _messageProcessor = messageProcessor;
            _logger = logger;
            _processorId = GenerateProcessorId();
        }

        /// <summary>
        /// Starts the processor service and connects to dispatcher
        /// </summary>
        public async Task StartAsync()
        {
            using var call = _client.Connect();
            var responseStream = call.ResponseStream;
            var requestStream = call.RequestStream;

            // Send introduction message
            var introduction = new Protos.ProcessorMessage
            {
                Introduction = new Protos.Introduction
                {
                    Id = _processorId,
                    Type = "RegexEngine"
                }
            };

            await requestStream.WriteAsync(introduction);
            _logger.LogInformation($"Processor {_processorId} introduced to dispatcher");

            // Handle incoming messages and send results
            await foreach (var message in responseStream.ReadAllAsync())
            {
                try
                {
                    // ✅ استفاده از DispatcherMessage به جای ProcessorMessage برای ContentCase
                    switch (message.ContentCase)
                    {
                        case Protos.DispatcherMessage.ContentOneofCase.Message:
                            await ProcessIncomingMessageAsync(message.Message, requestStream);
                            break;
                        case Protos.DispatcherMessage.ContentOneofCase.Config:
                            _logger.LogInformation("Configuration received from dispatcher");
                            break;
                        default:
                            _logger.LogWarning($"Unknown message type: {message.ContentCase}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing dispatcher message");
                }
            }
        }

        private async Task ProcessIncomingMessageAsync(Protos.Message incomingMessage,
            IClientStreamWriter<Protos.ProcessorMessage> requestStream)
        {
            _logger.LogInformation($"Processing incoming message {incomingMessage.Id}");

            // Simulate processing delay
            await Task.Delay(100);

            // Convert Proto Message to Model Message
            var message = new Models.Message
            {
                Id = incomingMessage.Id,
                Sender = incomingMessage.Sender,
                Content = incomingMessage.Content
            };

            var regexRules = new Dictionary<string, string>
            {
                ["word_count"] = @"\b\w+\b",
                ["email"] = @"[\w\.-]+@[\w\.-]+\.\w+"
            };

            var processed = _messageProcessor.ProcessMessage(message, regexRules);

            // Create AdditionalFields wrapper
            var additionalFields = new Protos.AdditionalFields();
            foreach (var field in processed.AdditionalFields)
            {
                additionalFields.Fields[field.Key] = field.Value;
            }

            var resultMessage = new Protos.ProcessorMessage
            {
                Result = new Protos.ResultMessage
                {
                    Id = processed.Id,
                    Engine = processed.Engine,
                    MessageLength = processed.MessageLength,
                    IsValid = processed.IsValid,
                    AdditionalFields = additionalFields
                }
            };

            await requestStream.WriteAsync(resultMessage);
            _logger.LogInformation($"Result sent for message {incomingMessage.Id}");
        }

        private string GenerateProcessorId()
        {
            var macAddress = "AA:BB:CC:DD:EE:FF";
            return $"{Guid.NewGuid()}-{macAddress.GetHashCode():X}";
        }
    }
}