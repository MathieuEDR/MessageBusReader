using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Azure.Messaging.ServiceBus;
using MessageBusReader.DataTypes;

namespace MessageBusReader.Services;

internal static class OperationLogger
{
    private static int _completeCounter;
    private static int _messageCount;

    internal static void MessageReturned(TargetQueue queue)
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

    internal static void RecordMessageProcessing(ProcessMessageEventArgs message)
    {
        _messageCount = Interlocked.Increment(ref _messageCount);

        Console.WriteLine($"Processing {_messageCount}: {message.Message.MessageId}");
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
    private static readonly ConcurrentDictionary<string, int> MessageTypeCounts = new();

    public static void CountByMessageType(string? messageType)
    {
        var key = messageType ?? "Unknown";
        MessageTypeCounts.AddOrUpdate(key, 1, (_, count) => count + 1);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("--- Message Type Counts ---");
        foreach (var kvp in MessageTypeCounts.OrderBy(v => v.Value))
        {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
        Console.WriteLine("---------------------------");
        Console.ResetColor();
    }

    public static void MessageReturnedWithDelay(TargetQueue queue, int delayInSeconds)
    {

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"Returning message to queue {queue.Name} with delay of {delayInSeconds} seconds");
        Console.ResetColor();    }
}
