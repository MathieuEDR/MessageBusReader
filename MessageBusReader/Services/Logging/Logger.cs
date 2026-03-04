using System;

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
}
