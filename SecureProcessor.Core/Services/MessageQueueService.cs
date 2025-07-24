using Microsoft.Extensions.Logging;
using SecureProcessor.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Core.Services
{
    /// <summary>
    /// Implementation of message queue service with simulation
    /// </summary>
    public class MessageQueueService : IMessageQueueService
    {
        private readonly Random _random = new();
        private readonly ILogger<MessageQueueService> _logger;

        public MessageQueueService(ILogger<MessageQueueService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Simulates getting a message from queue with 200ms delay
        /// </summary>
        public async Task<Message> GetMessageAsync()
        {
            await Task.Delay(200); // Simulate queue delay

            var message = new Message
            {
                Id = _random.Next(1, 10000),
                Sender = _random.Next(0, 2) == 0 ? "Legal" : "Finance",
                Content = GenerateRandomContent()
            };

            _logger.LogInformation($"Retrieved message {message.Id} from queue");
            return message;
        }

        /// <summary>
        /// Simulates sending processed message to results queue
        /// </summary>
        public async Task SendProcessedMessageAsync(ProcessedMessage message)
        {
            await Task.CompletedTask; // Simulate async operation
            _logger.LogInformation($"Sent processed message {message.Id} to results queue");
        }

        private string GenerateRandomContent()
        {
            var words = new[] { "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit" };
            var content = new List<string>();

            var wordCount = _random.Next(3, 10);
            for (int i = 0; i < wordCount; i++)
            {
                content.Add(words[_random.Next(words.Length)]);
            }

            return string.Join(" ", content);
        }
    }
}
