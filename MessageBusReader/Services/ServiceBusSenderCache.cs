using System.Collections.Generic;
using Azure.Messaging.ServiceBus;

namespace MessageBusReader.Services;

internal class ServiceBusSenderCache(ServiceBusClient client)
{
    private static readonly Dictionary<string, ServiceBusSender> Senders = new();

    internal ServiceBusSender GetSender(string queueName)
    {
        if (Senders.TryGetValue(queueName, out var cachedSender))
        {
            // We already have one in cache, return that
            return cachedSender;
        }

        var sender = client.CreateSender(queueName);

        Senders.Add(queueName, sender);

        return sender;
    }
}
