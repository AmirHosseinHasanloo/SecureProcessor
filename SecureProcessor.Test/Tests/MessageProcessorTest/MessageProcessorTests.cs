using Microsoft.Extensions.Logging;
using Moq;
using SecureProcessor.Processor.Services;
using SecureProcessor.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureProcessor.Test.Tests.MessageProcessorTest
{
    public class MessageProcessorServiceTests
    {
        private readonly Mock<ILogger<MessageProcessorService>> _mockLogger;
        private readonly MessageProcessorService _service;

        public MessageProcessorServiceTests()
        {
            _mockLogger = new Mock<ILogger<MessageProcessorService>>();
            _service = new MessageProcessorService(_mockLogger.Object);
        }

        [Fact]
        public void ProcessMessage_Should_Calculate_Length_Correctly()
        {
            // Arrange
            var message = new Message { Id = 1, Content = "Hello World" };
            var rules = new Dictionary<string, string>();

            // Act
            var result = _service.ProcessMessage(message, rules);

            // Assert
            Assert.Equal(11, result.MessageLength);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ProcessMessage_Should_Apply_Regex_Rules()
        {
            // Arrange
            var message = new Message { Id = 1, Content = "Contact us at test@example.com" };
            var rules = new Dictionary<string, string>
            {
                ["email"] = @"[\w\.-]+@[\w\.-]+\.\w+"
            };

            // Act
            var result = _service.ProcessMessage(message, rules);

            // Assert
            Assert.True(result.AdditionalFields["email"]);
        }

        [Fact]
        public void ProcessMessage_Should_Handle_Invalid_Regex()
        {
            // Arrange
            var message = new Message { Id = 1, Content = "test" };
            var rules = new Dictionary<string, string>
            {
                ["invalid"] = @"["
            };

            // Act
            var result = _service.ProcessMessage(message, rules);

            // Assert
            Assert.False(result.AdditionalFields["invalid"]);
        }
    }
}
