using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.DataTypes;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.Services.Logging;
using MessageBusReader.Services.ServiceBus;

namespace MessageBusReader.Extensions;

internal static class ProcessMessageEventArgsExtensions
{
    private static readonly Logger Logger = new();

    internal static MessageType? GetType(this ProcessMessageEventArgs messageEvent)
    {
        if (messageEvent.Message.ApplicationProperties.TryGetValue("rbs2-msg-type", out var rawValue))
        {
            if (rawValue?.ToString() is { } sanitizedValue)
            {
                return new MessageType(sanitizedValue);
            }
        }

        return null;
    }

    internal static bool IsOfType(this ProcessMessageEventArgs messageEvent, params string[] targetMessageTypes)
    {
        if (GetType(messageEvent) is { } actualMessageType)
        {
            return targetMessageTypes.Any(targetMessageType => actualMessageType.Value == targetMessageType);
        }

        return false;
    }

    internal static Queue? GetSourceQueue(this ProcessMessageEventArgs messageEvent)
    {
        if (!messageEvent.Message.ApplicationProperties.TryGetValue("rbs2-source-queue", out var rawValue))
        {
            return null;
        }

        if (rawValue?.ToString() is { } sanitizedValue)
        {
            var queueName = new QueueName(sanitizedValue);
            return new Queue(queueName);
        }

        return null;
    }

    internal static async Task ReturnToSourceQueue(this ProcessMessageEventArgs messageEvent, int delayInSeconds = 0)
    {
        var source = messageEvent.GetSourceQueue();

        if (source == null)
        {
            OperationLogger.Error("Message does not have a source queue property");
            return;
        }

        var copy = new ServiceBusMessage(messageEvent.Message);

        await SendToQueue(copy, source, delayInSeconds);

        await Delete(messageEvent);
    }

    internal static async Task SendToQueue(this ServiceBusMessage message, Queue queue, int delayInSeconds = 0)
    {
        var sender = ServiceBusSenderCache.GetSender(queue);

        if (delayInSeconds > 0)
        {
            await sender.ScheduleMessageAsync(message, DateTimeOffset.UtcNow.AddSeconds(delayInSeconds));
            Logger.LogMessageSentWithDelay(message, queue, delayInSeconds);
        }
        else
        {
            await sender.SendMessageAsync(message);
            Logger.LogMessageSent(message, queue);
        }
    }

    internal static async Task Delete(this ProcessMessageEventArgs messageEvent)
    {
        await messageEvent.CompleteMessageAsync(messageEvent.Message);

        OperationLogger.MessageCompleted();
    }

    internal static string Deserialize(this ProcessMessageEventArgs message)
    {
        return Encoding.UTF8.GetString(message.Message.Body);
    }
}
