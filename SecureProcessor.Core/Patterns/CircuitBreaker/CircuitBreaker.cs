using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Core.Patterns.CircuitBreaker
{
    /// <summary>
    /// Implementation of circuit breaker pattern to prevent cascading failures
    /// </summary>
    public class CircuitBreaker : ICircuitBreaker
    {
        private readonly ILogger<CircuitBreaker> _logger;
        private readonly TimeSpan _timeout;
        private readonly int _failureThreshold;
        private readonly TimeSpan _retryPeriod;

        private int _failureCount;
        private DateTime _lastFailureTime;
        private CircuitState _state;

        public enum CircuitState
        {
            Closed,
            Open,
            HalfOpen
        }

        public CircuitBreaker(ILogger<CircuitBreaker> logger,
            TimeSpan timeout = default,
            int failureThreshold = 5,
            TimeSpan retryPeriod = default)
        {
            _logger = logger;
            _timeout = timeout == default ? TimeSpan.FromMinutes(1) : timeout;
            _failureThreshold = failureThreshold;
            _retryPeriod = retryPeriod == default ? TimeSpan.FromMinutes(1) : retryPeriod;
            _state = CircuitState.Closed;
        }

        /// <summary>
        /// Executes an operation with circuit breaker protection
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            if (_state == CircuitState.Open)
            {
                if (DateTime.UtcNow - _lastFailureTime < _retryPeriod)
                {
                    throw new CircuitBreakerOpenException("Circuit breaker is open");
                }
                _state = CircuitState.HalfOpen;
            }

            try
            {
                var result = await operation();
                OnSuccess();
                return result;
            }
            catch (Exception ex)
            {
                OnFailure(ex);
                throw;
            }
        }

        /// <summary>
        /// Executes an operation without return value with circuit breaker protection
        /// </summary>
        public async Task ExecuteAsync(Func<Task> operation)
        {
            if (_state == CircuitState.Open)
            {
                if (DateTime.UtcNow - _lastFailureTime < _retryPeriod)
                {
                    throw new CircuitBreakerOpenException("Circuit breaker is open");
                }
                _state = CircuitState.HalfOpen;
            }

            try
            {
                await operation();
                OnSuccess();
            }
            catch (Exception ex)
            {
                OnFailure(ex);
                throw;
            }
        }

        private void OnSuccess()
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
            _logger.LogInformation("Circuit breaker closed - operation succeeded");
        }

        private void OnFailure(Exception ex)
        {
            _failureCount++;
            _lastFailureTime = DateTime.UtcNow;

            _logger.LogWarning($"Circuit breaker failure #{_failureCount}: {ex.Message}");

            if (_failureCount >= _failureThreshold)
            {
                _state = CircuitState.Open;
                _logger.LogError("Circuit breaker opened due to repeated failures");
            }
        }
    }
}
