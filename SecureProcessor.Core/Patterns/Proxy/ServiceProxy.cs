using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SecureProcessor.Core.Patterns.CircuitBreaker;

namespace SecureProcessor.Core.Patterns.Proxy
{
    /// <summary>
    /// Implementation of service proxy with circuit breaker and retry patterns
    /// </summary>
    /// <typeparam name="T">Service contract type</typeparam>
    public class ServiceProxy<T> : IServiceProxy<T> where T : class
    {
        private readonly T _service;
        private readonly ICircuitBreaker _circuitBreaker;
        private readonly ILogger<ServiceProxy<T>> _logger;

        public ServiceProxy(T service, ICircuitBreaker circuitBreaker, ILogger<ServiceProxy<T>> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _circuitBreaker = circuitBreaker ?? throw new ArgumentNullException(nameof(circuitBreaker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes a service operation with circuit breaker and retry logic
        /// </summary>
        public async Task<TResult> InvokeAsync<TResult>(Func<T, Task<TResult>> operation,
            int retryCount = 3,
            TimeSpan retryDelay = default)
        {
            if (retryDelay == default)
                retryDelay = TimeSpan.FromSeconds(1);

            return await _circuitBreaker.ExecuteAsync(async () =>
            {
                for (int attempt = 0; attempt <= retryCount; attempt++)
                {
                    try
                    {
                        return await operation(_service);
                    }
                    catch (Exception ex) when (attempt < retryCount)
                    {
                        _logger.LogWarning($"Attempt {attempt + 1} failed: {ex.Message}. Retrying in {retryDelay.TotalSeconds}s...");
                        await Task.Delay(retryDelay);
                    }
                }
                throw new InvalidOperationException("All retry attempts failed");
            });
        }

        /// <summary>
        /// Executes a service operation without return value with circuit breaker and retry logic
        /// </summary>
        public async Task InvokeAsync(Func<T, Task> operation,
            int retryCount = 3,
            TimeSpan retryDelay = default)
        {
            if (retryDelay == default)
                retryDelay = TimeSpan.FromSeconds(1);

            await _circuitBreaker.ExecuteAsync(async () =>
            {
                for (int attempt = 0; attempt <= retryCount; attempt++)
                {
                    try
                    {
                        await operation(_service);
                        return;
                    }
                    catch (Exception ex) when (attempt < retryCount)
                    {
                        _logger.LogWarning($"Attempt {attempt + 1} failed: {ex.Message}. Retrying in {retryDelay.TotalSeconds}s...");
                        await Task.Delay(retryDelay);
                    }
                }
                throw new InvalidOperationException("All retry attempts failed");
            });
        }
    }
}
