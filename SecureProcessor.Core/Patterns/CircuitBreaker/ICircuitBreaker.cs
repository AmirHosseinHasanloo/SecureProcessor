using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Core.Patterns.CircuitBreaker
{
    /// <summary>
    /// Circuit breaker pattern interface for handling service failures
    /// </summary>
    public interface ICircuitBreaker
    {
        /// <summary>
        /// Executes an operation with circuit breaker protection
        /// </summary>
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation);

        /// <summary>
        /// Executes an operation without return value with circuit breaker protection
        /// </summary>
        Task ExecuteAsync(Func<Task> operation);
    }
}
