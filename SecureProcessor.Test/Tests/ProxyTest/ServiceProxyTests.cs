using Microsoft.Extensions.Logging;
using Moq;
using SecureProcessor.Core.Patterns.CircuitBreaker;
using SecureProcessor.Core.Patterns.Proxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Test.Tests.ProxyTest
{
    public class ServiceProxyTests
    {
        private readonly Mock<ILogger<ServiceProxy<ITestService>>> _mockLogger;
        private readonly Mock<ICircuitBreaker> _mockCircuitBreaker;
        private readonly Mock<ITestService> _mockService;
        private readonly ServiceProxy<ITestService> _proxy;

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
            _proxy = new ServiceProxy<ITestService>(_mockService.Object, _mockCircuitBreaker.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task InvokeAsync_WithResult_Should_Call_Service_Method()
        {
            // Arrange
            var expectedResult = "test result";
            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(func => func());

            _mockService.Setup(s => s.GetDataAsync()).ReturnsAsync(expectedResult);

            // Act
            var result = await _proxy.InvokeAsync(s => s.GetDataAsync());

            // Assert
            Assert.Equal(expectedResult, result);
            _mockService.Verify(s => s.GetDataAsync(), Times.Once);
        }

        [Fact]
        public async Task InvokeAsync_WithoutResult_Should_Call_Service_Method()
        {
            // Arrange
            _mockCircuitBreaker.Setup(cb => cb.ExecuteAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(func => func());

            // Act
            await _proxy.InvokeAsync(s => s.DoWorkAsync());

            // Assert
            _mockService.Verify(s => s.DoWorkAsync(), Times.Once);
        }
    }
}
