using SecureProcessor.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Processor.Services
{
    /// <summary>
    /// Interface for message processing operations
    /// </summary>
    public interface IMessageProcessorService
    {
        /// <summary>
        /// Processes a message with given regex rules
        /// </summary>
        ProcessedMessage ProcessMessage(Message message, Dictionary<string, string> regexRules);
    }
}
