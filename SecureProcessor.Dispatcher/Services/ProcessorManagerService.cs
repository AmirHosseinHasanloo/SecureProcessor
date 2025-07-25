
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureProcessor.Shared.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Dispatcher.Services
{
    /// <summary>
    /// Implementation of processor manager service
    /// </summary>
    public class ProcessorManagerService : IProcessorManagerService
    {
        private readonly Dictionary<string, ProcessorInfo> _processors = new();
        private readonly object _lock = new();
        private readonly ILogger<ProcessorManagerService> _logger;
        private readonly int _maxActiveProcessors;

      

        public ProcessorManagerService(ILogger<ProcessorManagerService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _maxActiveProcessors = configuration.GetValue<int>("MaxActiveProcessors", 5);
        }

        /// <summary>
        /// Registers a new processor connection
        /// </summary>
        public async Task RegisterProcessorAsync(string processorId, string processorType)
        {
            await Task.CompletedTask; // Simulate async operation

            lock (_lock)
            {
                var isActive = _processors.Values.Count(p => p.IsActive) < _maxActiveProcessors;

                _processors[processorId] = new ProcessorInfo
                {
                    Id = processorId,
                    Type = processorType,
                    IsActive = isActive,
                    LastActivity = DateTime.UtcNow
                };

                _logger.LogInformation($"Processor {processorId} registered. Active: {isActive}");
            }
        }

        /// <summary>
        /// Unregisters a processor connection
        /// </summary>
        public async Task UnregisterProcessorAsync(string processorId)
        {
            await Task.CompletedTask; // Simulate async operation

            lock (_lock)
            {
                if (_processors.Remove(processorId))
                {
                    _logger.LogInformation($"Processor {processorId} unregistered");
                }
            }
        }

        /// <summary>
        /// Gets active processors count
        /// </summary>
        public int GetActiveProcessorsCount()
        {
            lock (_lock)
            {
                return _processors.Values.Count(p => p.IsActive);
            }
        }

        /// <summary>
        /// Cleans up inactive processors (older than 5 minutes)
        /// </summary>
        public async Task CleanupInactiveProcessorsAsync()
        {
            await Task.CompletedTask; // Simulate async operation

            var cutoffTime = DateTime.UtcNow.AddMinutes(-5);
            var inactiveProcessors = new List<string>();

            lock (_lock)
            {
                inactiveProcessors = _processors
                    .Where(p => p.Value.LastActivity < cutoffTime)
                    .Select(p => p.Key)
                    .ToList();
            }

            foreach (var processorId in inactiveProcessors)
            {
                await UnregisterProcessorAsync(processorId);
                _logger.LogInformation($"Cleaned up inactive processor {processorId}");
            }
        }
    }
}