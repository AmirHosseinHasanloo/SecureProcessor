using SecureProcessor.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Dispatcher.Services
{
    /// <summary>
    /// Interface for health check service operations
    /// </summary>
    public interface IHealthCheckService
    {
        /// <summary>
        /// Performs health check with retry logic and circuit breaker
        /// </summary>
        Task<HealthCheckResponse> CheckHealthAsync(string managerUrl);
    }
}
