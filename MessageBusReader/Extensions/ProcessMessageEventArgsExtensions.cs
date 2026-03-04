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
    internal static MessageType? GetMessageType(this ProcessMessageEventArgs messageEvent)
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
        if (messageEvent.GetMessageType() is { } actualMessageType)
        {
            return targetMessageTypes.Any(targetMessageType => actualMessageType.Value == targetMessageType);
        }

        return false;
    }

    internal static TargetQueue? GetMessageSourceQueue(this ProcessMessageEventArgs messageEvent)
    {
        if (!messageEvent.Message.ApplicationProperties.TryGetValue("rbs2-source-queue", out var rawValue))
        {
            return null;
        }

        if (rawValue?.ToString() is { } sanitizedValue)
        {
            var queueName = new QueueName(sanitizedValue);
            return new TargetQueue(queueName);
        }

        return null;
    }

    internal static async Task ReturnMessageToSourceQueue(this ProcessMessageEventArgs messageEvent, int delayInSeconds = 0)
    {
        var source = messageEvent.GetMessageSourceQueue();

        if (source == null)
        {
            OperationLogger.Error("Message does not have a source queue property");
            return;
        }

        var copy = new ServiceBusMessage(messageEvent.Message);

        await ReturnMessageToQueue(copy, source, delayInSeconds);

        await CompleteMessage(messageEvent);
    }

    internal static async Task ReturnMessageToQueue(this ServiceBusMessage message, TargetQueue queueName, int delayInSeconds = 0)
    {
        var sender = ServiceBusSenderCache.GetSender(queueName);

        if (delayInSeconds > 0)
        {
            await sender.ScheduleMessageAsync(message, DateTimeOffset.UtcNow.AddSeconds(delayInSeconds));
            OperationLogger.MessageReturnedWithDelay(queueName, delayInSeconds);
        }
        else
        {
            await sender.SendMessageAsync(message);
            OperationLogger.MessageReturned(queueName);
        }
    }

    internal static async Task CompleteMessage(this ProcessMessageEventArgs messageEvent)
    {
        await messageEvent.CompleteMessageAsync(messageEvent.Message);

        OperationLogger.MessageCompleted();
    }

    internal static string Deserialize(this ProcessMessageEventArgs message)
    {
        return Encoding.UTF8.GetString(message.Message.Body);
    }
}
