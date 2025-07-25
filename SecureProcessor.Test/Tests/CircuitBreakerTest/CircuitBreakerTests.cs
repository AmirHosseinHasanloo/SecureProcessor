using Microsoft.Extensions.Logging;
using Moq;
using SecureProcessor.Core.Patterns.CircuitBreaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Test.Tests.CircuitBreakerTest
{
    public class CircuitBreakerTests
    {
        private readonly Mock<ILogger<CircuitBreaker>> _mockLogger;
        private readonly CircuitBreaker _circuitBreaker;

        public CircuitBreakerTests()
        {
            _mockLogger = new Mock<ILogger<CircuitBreaker>>();
            _circuitBreaker = new CircuitBreaker(_mockLogger.Object,
                failureThreshold: 3,
                retryPeriod: TimeSpan.FromMilliseconds(100));
        }

        [Fact]
        public async Task ExecuteAsync_Should_Succeed_When_Operation_Succeeds()
        {
            // Arrange
            var result = "success";
            Func<Task<string>> operation = () => Task.FromResult(result);

            // Act
            var actualResult = await _circuitBreaker.ExecuteAsync(operation);

            // Assert
            Assert.Equal(result, actualResult);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Open_Circuit_After_Threshold_Exceeded()
        {
            // Arrange
            Func<Task<string>> failingOperation = () => throw new InvalidOperationException("Test failure");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));

            // Circuit should be open now
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
        }
    }
}
