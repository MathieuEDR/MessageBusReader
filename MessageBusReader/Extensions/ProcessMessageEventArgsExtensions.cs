using Azure.Messaging.ServiceBus;

namespace MessageBusReader.Extensions;

internal static class ProcessMessageEventArgsExtensions
{
    internal static string? GetMessageType(this ProcessMessageEventArgs messageEvent)
    {
        if (messageEvent.Message.ApplicationProperties.TryGetValue("rbs2-msg-type", out var value))
        {
            return value?.ToString();
        }

        return null;
    }  
    
    internal static string? GetMessageSourceQueue(this ProcessMessageEventArgs messageEvent)
    {
        if (messageEvent.Message.ApplicationProperties.TryGetValue("rbs2-source-queue", out var value))
        {
            return value?.ToString();
        }

        return null;
    }
}