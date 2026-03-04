using System;
using Azure.Messaging.ServiceBus;
using MessageBusReader.DataTypes.Queue;

namespace MessageBusReader.Services.Logging;

internal class Logger(string? serviceName = null)
{
    public void Log(string message)
    {
        AddServiceNameIfPresent();

        Console.Write(message);
        Console.WriteLine();
    }

    public void LogError(string message)
    {
        AddServiceNameIfPresent();

        Console.ForegroundColor = ConsoleColor.Red;

        Console.WriteLine(message);
        Console.ResetColor();
    }

    private void AddServiceNameIfPresent()
    {
        if (serviceName != null)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write(serviceName);
            Console.Write(": ");
            Console.ResetColor();
        }
    }

    public void LogMessageSent(ServiceBusMessage message, Queue queue)
    {
        AddServiceNameIfPresent();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"Sent message {message.MessageId} to queue {queue}");
        Console.ResetColor();
    }

    public void LogMessageSentWithDelay(ServiceBusMessage message, Queue queue, int delayInSeconds)
    {

        AddServiceNameIfPresent();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.WriteLine($"Sent message {message.MessageId} to queue {queue} with delay of {delayInSeconds} seconds");
        Console.ResetColor();    }
}
