using SecureProcessor.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Core.Services
{
    /// <summary>
    /// Interface for message queue operations
    /// </summary>
    public interface IMessageQueueService
    {
        /// <summary>
        /// Gets a message from the queue with simulated delay
        /// </summary>
        Task<Message> GetMessageAsync();

        /// <summary>
        /// Sends processed message to results queue (simulated with logging)
        /// </summary>
        Task SendProcessedMessageAsync(ProcessedMessage message);
    }
}
