using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SecureProcessor.Core.Patterns.CircuitBreaker.Tests
{
    /// <summary>
    /// Unit tests for CircuitBreaker class
    /// Tests cover all circuit breaker states and behaviors including failure handling, 
    /// state transitions, and exception scenarios.
    /// </summary>
    public class CircuitBreakerTests
    {
        private readonly Mock<ILogger<CircuitBreaker>> _mockLogger;
        private readonly CircuitBreaker _circuitBreaker;

        public CircuitBreakerTests()
        {
            _mockLogger = new Mock<ILogger<CircuitBreaker>>();
            // Create circuit breaker with low threshold for testing
            _circuitBreaker = new CircuitBreaker(
                _mockLogger.Object,
                failureThreshold: 3,
                retryPeriod: TimeSpan.FromMilliseconds(100)
            );
        }

        /// <summary>
        /// Tests that a successful operation executes normally and keeps circuit closed
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithResult_Should_Execute_Successful_Operation()
        {
            // Arrange
            var expectedResult = "success";
            Func<Task<string>> operation = () => Task.FromResult(expectedResult);

            // Act
            var result = await _circuitBreaker.ExecuteAsync(operation);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(CircuitBreaker.CircuitState.Closed, GetCircuitState());
        }

        /// <summary>
        /// Tests that a successful void operation executes normally and keeps circuit closed
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithoutResult_Should_Execute_Successful_Void_Operation()
        {
            // Arrange
            var executed = false;
            Func<Task> operation = () =>
            {
                executed = true;
                return Task.CompletedTask;
            };

            // Act
            await _circuitBreaker.ExecuteAsync(operation);

            // Assert
            Assert.True(executed);
            Assert.Equal(CircuitBreaker.CircuitState.Closed, GetCircuitState());
        }

        /// <summary>
        /// Tests that circuit breaker opens after reaching failure threshold
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_Should_Open_Circuit_After_Failure_Threshold_Exceeded()
        {
            // Arrange
            Func<Task<string>> failingOperation = () => throw new InvalidOperationException("Test failure");

            // Act & Assert
            // First two failures - circuit should remain closed
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));

            Assert.Equal(CircuitBreaker.CircuitState.Closed, GetCircuitState());
            Assert.Equal(2, GetFailureCount());

            // Third failure - circuit should open
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));

            Assert.Equal(CircuitBreaker.CircuitState.Open, GetCircuitState());
            Assert.Equal(3, GetFailureCount());
        }

        /// <summary>
        /// Tests that circuit breaker throws CircuitBreakerOpenException when open
        /// </summary>
        /// <summary>
        /// Tests that circuit breaker throws CircuitBreakerOpenException when open
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_Should_Throw_CircuitBreakerOpenException_When_Circuit_Is_Open()
        {
            // Arrange - Open the circuit
            Func<Task<string>> failingOperation = () => throw new InvalidOperationException("Test failure");

            // Open the circuit by exceeding failure threshold
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await _circuitBreaker.ExecuteAsync(failingOperation);
                }
                catch
                {
                    // Ignore exceptions during setup
                }
            }

            // Verify circuit is open
            Assert.Equal(CircuitBreaker.CircuitState.Open, GetCircuitState());

            // Act & Assert - Try to execute while circuit is open
            await Assert.ThrowsAsync<CircuitBreakerOpenException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
        }
        

        /// <summary>
        /// Tests that circuit transitions to HalfOpen state after retry period
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_Should_Transition_To_HalfOpen_After_Retry_Period()
        {
            // Arrange - Open the circuit
            Func<Task<string>> failingOperation = () => throw new InvalidOperationException("Test failure");

            // Open the circuit
            for (int i = 0; i < 3; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
            }

            Assert.Equal(CircuitBreaker.CircuitState.Open, GetCircuitState());

            // Wait for retry period to expire
            await Task.Delay(150); // 150ms > 100ms retry period

            // Act - Should transition to HalfOpen and then throw original exception
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));

            // Circuit should remain open after another failure in HalfOpen state
            Assert.Equal(CircuitBreaker.CircuitState.Open, GetCircuitState());
        }

        /// <summary>
        /// Tests that circuit closes after successful operation in HalfOpen state
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_Should_Close_Circuit_After_Success_In_HalfOpen_State()
        {
            // Arrange - Open the circuit
            Func<Task<string>> failingOperation = () => throw new InvalidOperationException("Test failure");
            Func<Task<string>> successOperation = () => Task.FromResult("success");

            // Open the circuit
            for (int i = 0; i < 3; i++)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
            }

            Assert.Equal(CircuitBreaker.CircuitState.Open, GetCircuitState());

            // Wait for retry period to expire
            await Task.Delay(150);

            // Act - Successful operation should close the circuit
            var result = await _circuitBreaker.ExecuteAsync(successOperation);

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(CircuitBreaker.CircuitState.Closed, GetCircuitState());
            Assert.Equal(0, GetFailureCount());
        }

        /// <summary>
        /// Tests that failure count resets after successful operation
        /// </summary>
        [Fact]
        public async Task OnSuccess_Should_Reset_Failure_Count()
        {
            // Arrange - Cause some failures
            Func<Task<string>> failingOperation = () => throw new InvalidOperationException("Test failure");
            Func<Task<string>> successOperation = () => Task.FromResult("success");

            // Cause 2 failures
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));

            Assert.Equal(2, GetFailureCount());

            // Act - Successful operation
            await _circuitBreaker.ExecuteAsync(successOperation);

            // Assert - Failure count should be reset
            Assert.Equal(0, GetFailureCount());
            Assert.Equal(CircuitBreaker.CircuitState.Closed, GetCircuitState());
        }

        /// <summary>
        /// Tests that void operations also trigger failure counting
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_WithoutResult_Should_Count_Failures()
        {
            // Arrange
            Func<Task> failingOperation = () => throw new InvalidOperationException("Test failure");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));
            await Assert.ThrowsAsync<InvalidOperationException>(() => _circuitBreaker.ExecuteAsync(failingOperation));

            Assert.Equal(CircuitBreaker.CircuitState.Open, GetCircuitState());
            Assert.Equal(3, GetFailureCount());
        }

        /// <summary>
        /// Tests that different exception types are handled correctly
        /// </summary>
        [Fact]
        public async Task ExecuteAsync_Should_Handle_Different_Exception_Types()
        {
            // Arrange
            Func<Task<string>>[] failingOperations = new Func<Task<string>>[]
            {
        () => throw new ArgumentException("Argument error"),
        () => throw new InvalidOperationException("Invalid operation"),
        () => throw new NullReferenceException("Null reference")
            };

            // Act & Assert - Each exception should increment failure count
            foreach (var operation in failingOperations)
            {
                await Assert.ThrowsAnyAsync<Exception>(() => _circuitBreaker.ExecuteAsync(operation));
            }

            Assert.Equal(CircuitBreaker.CircuitState.Open, GetCircuitState());
            Assert.Equal(3, GetFailureCount());
        }
        #region Helper Methods

        /// <summary>
        /// Helper method to access private _state field for testing
        /// </summary>
        private CircuitBreaker.CircuitState GetCircuitState()
        {
            var field = typeof(CircuitBreaker).GetField("_state",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (CircuitBreaker.CircuitState)field.GetValue(_circuitBreaker);
        }

        /// <summary>
        /// Helper method to access private _failureCount field for testing
        /// </summary>
        private int GetFailureCount()
        {
            var field = typeof(CircuitBreaker).GetField("_failureCount",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (int)field.GetValue(_circuitBreaker);
        }

        #endregion
    }
}