using Grpc.Core.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Core.Patterns.CircuitBreaker
{
    /// <summary>
    /// Exception thrown when circuit breaker is open and operations are blocked
    /// </summary>
    /// 

    public class CircuitBreakerOpenException : Exception
    {
     private readonly ILogger<CircuitBreakerOpenException> logger;

        public CircuitBreakerOpenException(ILogger<CircuitBreakerOpenException> logger)
        {
            this.logger = logger;
        }

        public CircuitBreakerOpenException(string message) : base(message)
        {
            logger.LogError(message + " " + "Exception thrown when circuit breaker is open and operations are blocked");
        }
    }
}
