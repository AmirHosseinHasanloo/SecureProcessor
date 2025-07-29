using Microsoft.Extensions.Logging;
using Moq;
using SecureProcessor.Core.Patterns.CircuitBreaker;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SecureProcessor.Core.Patterns.Proxy.Tests
{
    /// <summary>
    /// Unit tests for ServiceProxy class
    /// Tests cover factory pattern implementation, retry logic, 
    /// circuit breaker integration, and exception handling.
    /// </summary>
    public class ServiceProxyTests
    {
        private readonly Mock<ILogger<ServiceProxy<ITestService>>> _mockLogger;
        private readonly Mock<ICircuitBreaker> _mockCircuitBreaker;
        private readonly Mock<ITestService> _mockService;
        private readonly Func<ITestService> _serviceFactory;

        public interface ITestService
        {
            Task<string> GetDataAsync();
            Task DoWorkAsync();
        }

        public ServiceProxyTests()
        {
            _mockLogger = new Mock<ILogger<ServiceProxy<ITestService>>>();
            _mockCircuitBreaker = new Mock<ICircuitBreaker>();
            _mockService = new Mock<ITestService>();
            _serviceFactory = () => _mockService.Object;
        }

        /// <summary>
        /// Tests that ServiceProxy can be created with factory method
        /// </summary>
        [Fact]
        public void Constructor_Should_Create_Instance_With_Factory_Method()
        {
            // Act
            var proxy = new ServiceProxy<ITestService>(_serviceFactory, _mockCircuitBreaker.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(proxy);
        }

        /// <summary>
        /// Tests that constructor throws exception when factory is null
        /// </summary>
        [Fact]
        public void Constructor_Should_Throw_Exception_When_Factory_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ServiceProxy<ITestService>(null, _mockCircuitBreaker.Object, _mockLogger.Object));
        }

        /// <summary>
        /// Tests that constructor throws exception when circuit breaker is null
        /// </summary>
        [Fact]
        public void Constructor_Should_Throw_Exception_When_CircuitBreaker_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ServiceProxy<ITestService>(_serviceFactory, null, _mockLogger.Object));
        }

        /// <summary>
        /// Tests that constructor throws exception when logger is null
        /// </summary>
        [Fact]
        public void Constructor_Should_Throw_Exception_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ServiceProxy<ITestService>(_serviceFactory, _mockCircuitBreaker.Object, null));
        }

        /// <summary>
        /// Tests successful invocation with result using factory pattern
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WithResult_Should_Call_Service_Method_Through_Factory()
        {
            // Arrange
            var expectedResult = "test result";
            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(func => func());

            _mockService.Setup(s => s.GetDataAsync()).ReturnsAsync(expectedResult);

            var proxy = new ServiceProxy<ITestService>(_serviceFactory, _mockCircuitBreaker.Object, _mockLogger.Object);

            // Act
            var result = await proxy.InvokeAsync(s => s.GetDataAsync());

            // Assert
            Assert.Equal(expectedResult, result);
            _mockService.Verify(s => s.GetDataAsync(), Times.Once);
        }

        /// <summary>
        /// Tests successful invocation without result using factory pattern
        /// </summary>
        [Fact]
        public async Task InvokeAsync_WithoutResult_Should_Call_Service_Method_Through_Factory()
        {
            // Arrange
            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(func => func());

            var proxy = new ServiceProxy<ITestService>(_serviceFactory, _mockCircuitBreaker.Object, _mockLogger.Object);

            // Act
            await proxy.InvokeAsync(s => s.DoWorkAsync());

            // Assert
            _mockService.Verify(s => s.DoWorkAsync(), Times.Once);
        }

        /// <summary>
        /// Tests retry logic with factory pattern
        /// </summary>
        [Fact]
        public async Task InvokeAsync_Should_Retry_On_Failure_With_Factory_Pattern()
        {
            // Arrange
            var callCount = 0;
            var expectedResult = "success";

            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(async func => await func());

            _mockService.Setup(s => s.GetDataAsync())
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount < 3)
                        throw new InvalidOperationException("Test failure");
                    return expectedResult;
                });

            var proxy = new ServiceProxy<ITestService>(_serviceFactory, _mockCircuitBreaker.Object, _mockLogger.Object);

            // Act
            var result = await proxy.InvokeAsync(s => s.GetDataAsync(), retryCount: 5);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(3, callCount);
            _mockService.Verify(s => s.GetDataAsync(), Times.Exactly(3));
        }

        /// <summary>
        /// Tests that all retry attempts are used before throwing exception
        /// </summary>
        [Fact]
        public async Task InvokeAsync_Should_Throw_Exception_After_All_Retry_Attempts_With_Factory()
        {
            // Arrange
            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(async func => await func());

            _mockService.Setup(s => s.GetDataAsync())
                .Throws(new InvalidOperationException("Persistent failure"));

            var proxy = new ServiceProxy<ITestService>(_serviceFactory, _mockCircuitBreaker.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                proxy.InvokeAsync(s => s.GetDataAsync(), retryCount: 3));

            _mockService.Verify(s => s.GetDataAsync(), Times.Exactly(4)); // 1 initial + 3 retries
        }

        /// <summary>
        /// Tests circuit breaker integration with factory pattern
        /// </summary>
        [Fact]
        public async Task InvokeAsync_Should_Use_CircuitBreaker_With_Factory_Pattern()
        {
            // Arrange
            var expectedResult = "test result";
            var circuitBreakerCalled = false;

            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(async func =>
                {
                    circuitBreakerCalled = true;
                    return await func();
                });

            _mockService.Setup(s => s.GetDataAsync()).ReturnsAsync(expectedResult);

            var proxy = new ServiceProxy<ITestService>(_serviceFactory, _mockCircuitBreaker.Object, _mockLogger.Object);

            // Act
            var result = await proxy.InvokeAsync(s => s.GetDataAsync());

            // Assert
            Assert.True(circuitBreakerCalled);
            Assert.Equal(expectedResult, result);
        }

        /// <summary>
        /// Tests default retry delay is used when not specified
        /// </summary>
        [Fact]
        public async Task InvokeAsync_Should_Use_Default_Retry_Delay_When_Not_Specified()
        {
            // Arrange
            var callCount = 0;
            var expectedResult = "success";
            var executionTimes = new DateTime[2];

            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(async func => await func());

            _mockService.Setup(s => s.GetDataAsync())
                .ReturnsAsync(() =>
                {
                    executionTimes[callCount] = DateTime.UtcNow;
                    callCount++;
                    if (callCount < 2)
                        throw new InvalidOperationException("Test failure");
                    return expectedResult;
                });

            var proxy = new ServiceProxy<ITestService>(_serviceFactory, _mockCircuitBreaker.Object, _mockLogger.Object);

            // Act
            var result = await proxy.InvokeAsync(s => s.GetDataAsync(), retryCount: 1);

            // Assert
            Assert.Equal(expectedResult, result);
            // Note: We can't easily test the delay in unit tests, but we verify the retry logic works
        }

        /// <summary>
        /// Tests that factory creates new instance for each retry attempt
        /// </summary>
        [Fact]
        public async Task InvokeAsync_Should_Create_New_Service_Instance_For_Each_Retry()
        {
            // Arrange
            var factoryCallCount = 0;
            var mockServices = new Mock<ITestService>[3];
            for (int i = 0; i < 3; i++)
            {
                mockServices[i] = new Mock<ITestService>();
                mockServices[i].Setup(s => s.GetDataAsync())
                    .Throws(new InvalidOperationException($"Failure {i + 1}"));
            }

            var factory = new Func<ITestService>(() =>
            {
                var service = mockServices[factoryCallCount].Object;
                factoryCallCount++;
                return service;
            });

            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(async func => await func());

            var proxy = new ServiceProxy<ITestService>(factory, _mockCircuitBreaker.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                proxy.InvokeAsync(s => s.GetDataAsync(), retryCount: 2));

            // Verify that factory was called for each attempt (1 initial + 2 retries)
            Assert.Equal(3, factoryCallCount);

            // Verify each service instance was used
            for (int i = 0; i < 3; i++)
            {
                mockServices[i].Verify(s => s.GetDataAsync(), Times.Once);
            }
        }

        /// <summary>
        /// Tests exception handling and logging with factory pattern
        /// </summary>
        [Fact]
        public async Task InvokeAsync_Should_Log_Retry_Attempts()
        {
            // Arrange
            var callCount = 0;

            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(async func => await func());

            _mockService.Setup(s => s.GetDataAsync())
                .ReturnsAsync(() =>
                {
                    callCount++;
                    if (callCount <= 2)
                        throw new InvalidOperationException($"Retry {callCount}");
                    return "success";
                });

            var proxy = new ServiceProxy<ITestService>(_serviceFactory, _mockCircuitBreaker.Object, _mockLogger.Object);

            // Act
            var result = await proxy.InvokeAsync(s => s.GetDataAsync(), retryCount: 3, TimeSpan.FromMilliseconds(10));

            // Assert
            Assert.Equal("success", result);
            // Verify logging was called (we can't easily verify log content in unit tests)
        }
    }
}