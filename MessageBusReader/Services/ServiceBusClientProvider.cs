using System;
using Azure.Messaging.ServiceBus;

namespace MessageBusReader.Services;

internal static class ServiceBusClientProvider
{
    private static ServiceBusClient? _serviceBusClient;

    internal static ServiceBusClient GetClient()
    {
        Console.WriteLine("Building Client");

        return _serviceBusClient ??= new ServiceBusClient("Endpoint=sb://prodedringtonservicebus.servicebus.windows.net/;SharedAccessKeyName=SharedKey;SharedAccessKey=sHAYVL6e+8TMSSIo0HhFkl1ffQ8YzoIpLsN2OQP0xww=");
    }
}
