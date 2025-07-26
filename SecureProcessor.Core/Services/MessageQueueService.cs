using Microsoft.Extensions.Logging;
using SecureProcessor.Core.Services;
using SecureProcessor.Shared.Models;
using SecureProcessor.Shared.Protos;
using System.Reflection;
using Message = SecureProcessor.Shared.Models.Message;
namespace SecureProcessor.Core.Services;
/// <summary>
/// Implementation of message queue service with external message support
/// </summary>
public class MessageQueueService : IMessageQueueService
{
    private readonly Random _random = new();
    private readonly ILogger<MessageQueueService> _logger;

    public MessageQueueService(ILogger<MessageQueueService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets a message from the queue (prioritizes external messages)
    /// </summary>
    public async Task<Message> GetMessageAsync()
    {


        if (ExternalMessageQueue.TryDequeueExternalMessage(out var externalMessage))
        {
            _logger.LogInformation($"📤 DEQUEUED EXTERNAL MESSAGE: {externalMessage.Message.Id}");
            _logger.LogInformation($"   Request ID: {externalMessage.RequestId}");

            return externalMessage.Message;
        }

        // اگر پیام خارجی نبود، پیام تصادفی تولید کن
        await Task.Delay(200); // Simulate queue delay

        var message = new Message
        {
            Id = _random.Next(10000, 99999),
            Sender = _random.Next(0, 2) == 0 ? "Legal" : "Finance",
            Content = GenerateRandomContent()
        };

        _logger.LogInformation($"📤 GENERATED RANDOM MESSAGE: {message.Id}");
        return message;
    }

    /// <summary>
    /// Sends processed message to results queue (simulated with logging)
    /// </summary>
    public async Task SendProcessedMessageAsync(ProcessedMessage message)
    {
        await Task.CompletedTask; // Simulate async operation
        _logger.LogInformation($"📥 SENT PROCESSED MESSAGE {message.Id} TO RESULTS QUEUE");
        _logger.LogInformation($"   Engine: {message.Engine}");
        _logger.LogInformation($"   Length: {message.MessageLength}");
        _logger.LogInformation($"   Valid: {message.IsValid}");
    }

    private string GenerateRandomContent()
    {
        var words = new[] { "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit" };
        var content = new List<string>();

        var wordCount = _random.Next(3, 10);
        for (int i = 0; i < wordCount; i++)
        {
            content.Add(words[_random.Next(words.Length)]);
        }

        return string.Join(" ", content);
    }
}