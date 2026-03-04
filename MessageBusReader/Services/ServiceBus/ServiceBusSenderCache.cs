using System.Collections.Generic;
using Azure.Messaging.ServiceBus;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.Services.Logging;
using MessageBusReader.Services.Processors;

namespace MessageBusReader.Services.ServiceBus;

internal class ServiceBusSenderCache
{
    private static readonly Logger Logger = new(nameof(QueueProcessor));

    private static readonly Dictionary<string, ServiceBusSender> SendersCache = new();

    internal static ServiceBusSender GetSender(Queue targetQueue)
    {
        if (SendersCache.TryGetValue(targetQueue.Name.Name, out var cachedSender))
        {
            // We already have one in cache, return that
            return cachedSender;
        }

        Logger.Log("Building new sender");
        var sender = ServiceBusClientProvider.GetClient().CreateSender(targetQueue.Name.Name);

        SendersCache.Add(targetQueue.Name.Name, sender);

        return sender;
    }
}
