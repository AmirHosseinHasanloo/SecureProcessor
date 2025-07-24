using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Core.Patterns.Proxy
{
    /// <summary>
    /// Generic service proxy interface for remote service calls
    /// </summary>
    /// <typeparam name="T">Service contract type</typeparam>
    public interface IServiceProxy<T> where T : class
    {
        /// <summary>
        /// Invokes a method on the remote service with circuit breaker and retry logic
        /// </summary>
        Task<TResult> InvokeAsync<TResult>(Func<T, Task<TResult>> operation,
            int retryCount = 3,
            TimeSpan retryDelay = default);

        /// <summary>
        /// Invokes a method on the remote service without return value
        /// </summary>
        Task InvokeAsync(Func<T, Task> operation,
            int retryCount = 3,
            TimeSpan retryDelay = default);
    }
}
