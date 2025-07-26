using Models = SecureProcessor.Shared.Models;
using System.Collections.Concurrent;

namespace SecureProcessor.Core.Services;

public static class ExternalMessageQueue
{
    internal static readonly ConcurrentQueue<ExternalMessageWrapper> ExternalMessages = new();
    private static readonly object _queueLock = new object();

    public static bool TryDequeueExternalMessage(out ExternalMessageWrapper message)
    {
        lock (_queueLock)
        {
            return ExternalMessages.TryDequeue(out message);
        }
    }

    public static void EnqueueExternalMessage(ExternalMessageWrapper message)
    {
        lock (_queueLock)
        {
            ExternalMessages.Enqueue(message);
        }
    }

    public static int GetExternalMessageCount()
    {
        lock (_queueLock)
        {
            return ExternalMessages.Count;
        }
    }
}

public class ExternalMessageWrapper
{
    public Models.Message Message { get; set; }
    public string RequestId { get; set; }
    public string RequesterId { get; set; }
    public DateTime ReceivedTime { get; set; }
}