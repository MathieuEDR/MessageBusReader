using System.Collections.Generic;
using Azure.Messaging.ServiceBus;
using MessageBusReader.DataTypes;

namespace MessageBusReader.Services;

internal class ServiceBusSenderCache()
{
    private static readonly Dictionary<string, ServiceBusSender> SendersCache = new();

    internal static ServiceBusSender GetSender(TargetQueue targetQueue)
    {      

        if (SendersCache.TryGetValue(targetQueue.Name.Name, out var cachedSender))
        {
            // We already have one in cache, return that
            return cachedSender;
        }

        var sender = ServiceBusClientProvider.GetClient().CreateSender(targetQueue.Name.Name);

        SendersCache.Add(targetQueue.Name.Name, sender);

        return sender;
    }
}
