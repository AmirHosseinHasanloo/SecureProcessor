using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Dispatcher.Services
{
    /// <summary>
    /// Interface for managing connected processors
    /// </summary>
    public interface IProcessorManagerService
    {
        /// <summary>
        /// Registers a new processor connection
        /// </summary>
        Task RegisterProcessorAsync(string processorId, string processorType);

        /// <summary>
        /// Unregisters a processor connection
        /// </summary>
        Task UnregisterProcessorAsync(string processorId);

        /// <summary>
        /// Gets active processors count
        /// </summary>
        int GetActiveProcessorsCount();

        /// <summary>
        /// Cleans up inactive processors
        /// </summary>
        Task CleanupInactiveProcessorsAsync();
    }
}
