using Microsoft.Extensions.Logging;
using SecureProcessor.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SecureProcessor.Processor.Services
{
    /// <summary>
    /// Implementation of message processor service
    /// </summary>
    public class MessageProcessorService : IMessageProcessorService
    {
        private readonly ILogger<MessageProcessorService> _logger;

        public MessageProcessorService(ILogger<MessageProcessorService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Processes a message by calculating length and applying regex rules
        /// </summary>
        public ProcessedMessage ProcessMessage(Message message, Dictionary<string, string> regexRules)
        {
            _logger.LogInformation($"Processing message {message.Id}");

            var result = new ProcessedMessage
            {
                Id = message.Id,
                Engine = "RegexEngine",
                MessageLength = message.Content?.Length ?? 0,
                IsValid = !string.IsNullOrEmpty(message.Content),
                AdditionalFields = new Dictionary<string, bool>()
            };

            if (regexRules != null && message.Content != null)
            {
                foreach (var rule in regexRules)
                {
                    try
                    {
                        var regex = new Regex(rule.Value, RegexOptions.IgnoreCase);
                        result.AdditionalFields[rule.Key] = regex.IsMatch(message.Content);
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning($"Invalid regex pattern '{rule.Value}' for field '{rule.Key}': {ex.Message}");
                        result.AdditionalFields[rule.Key] = false;
                    }
                }
            }

            _logger.LogInformation($"Message {message.Id} processed successfully");
            return result;
        }
    }
}
