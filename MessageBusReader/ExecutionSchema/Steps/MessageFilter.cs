using Azure.Messaging.ServiceBus;
using MessageBusReader.Extensions;

namespace MessageBusReader.ExecutionSchema.Steps;

internal static class MessageFilter 
{
    internal static class Include
    {
        
        public static bool ForAll(ProcessMessageEventArgs message) => true;
        public static bool OfType(ProcessMessageEventArgs message,  params string[] targetMessageTypes) => message.IsOfType(targetMessageTypes);
    }
}