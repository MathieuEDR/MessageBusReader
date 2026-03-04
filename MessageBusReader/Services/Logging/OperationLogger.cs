using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.DataTypes;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.Services.Processors;

namespace MessageBusReader.Services.Logging;

internal static class OperationLogger
{    private static readonly Logger Logger = new(nameof(QueueProcessor));

    private static int _completeCounter;
    private static int _messageCount;

    internal static void MessageReturned(Queue queue)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"Returning message to queue {queue.Name}");
        Console.ResetColor();
    }

    internal static void MessageCompleted()
    {
        _completeCounter++;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"{_completeCounter} messages completed");
        Console.ResetColor();
    }

    internal static void MessageProcessingStarted(ProcessMessageEventArgs message)
    {
        _messageCount = Interlocked.Increment(ref _messageCount);

        Logger.Log($"Started processing {_messageCount}: {message.Message.MessageId}");
    }
    internal static void MessageProcessingFinished(ProcessMessageEventArgs message)
    {
        Logger.Log($"Finished processing {_messageCount}: {message.Message.MessageId}");
    }

    internal static void Warning(string messageString)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;

        Console.WriteLine(messageString);
        Console.ResetColor();
    }

    internal static void Error(string messageString)
    {
        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(messageString);
        Console.ResetColor();
    }

    private static ConcurrentBag<string?> DataPoints { get; set; } = new();

    public static void CollectDataPoint(string? orderNumber)
    {
        DataPoints.Add(orderNumber);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Collected order numbers: {string.Join(", ", DataPoints)}");
        Console.ResetColor();
    }

    private static readonly ConcurrentDictionary<MessageType, int> MessageTypeCounts = new();


    public static void MessageReturnedWithDelay(Queue queue, int delayInSeconds)
    {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"Returning message to queue {queue.Name} with delay of {delayInSeconds} seconds");
        Console.ResetColor();
    }

    public static Task CountByMessageType(MessageType messageType)
    {
        MessageTypeCounts.AddOrUpdate(messageType, 1, (_, count) => count + 1);

        Console.ForegroundColor = ConsoleColor.Cyan;
        
        var countOfMessagesForType = MessageTypeCounts.GetValueOrDefault(messageType);
        Console.WriteLine($"{countOfMessagesForType} messages of type {messageType} so far");
        
        Console.ResetColor();

        return Task.CompletedTask;
    }

    public static Task DisplayMessageTypeCount()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- Message Type Counts ---");
        
        foreach (var (messageType, count) in MessageTypeCounts.OrderBy(v => v.Value))
        {
            Console.WriteLine($"{messageType}: {count}");
        }

        Console.WriteLine("---------------------------");
        Console.ResetColor();

        return Task.CompletedTask;
    }
}
