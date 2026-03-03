using Azure.Messaging.ServiceBus;

namespace MessageBusReader.ExecutionSchema.Steps;

internal static class MessageFilter 
{
    public static bool ForAll(ProcessMessageEventArgs message) => true;
    public static bool OfType(ProcessMessageEventArgs message,  params string[] targetMessageTypes) => true;
}