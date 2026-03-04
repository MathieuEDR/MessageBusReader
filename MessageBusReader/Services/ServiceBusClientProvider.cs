using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace MessageBusReader.Services;

internal static class ServiceBusClientProvider
{
    
    private static readonly Logger Logger = new(nameof(QueueProcessor));

    private static ServiceBusClient? _serviceBusClient;

    internal static ServiceBusClient GetClient()
    {
        Console.WriteLine("Building Client");

        return _serviceBusClient ??= CreateNewClient();
    }

    private static ServiceBusClient CreateNewClient()
    {
        Logger.Log("Creating new client");
        return new ServiceBusClient("Endpoint=sb://prodedringtonservicebus.servicebus.windows.net/;SharedAccessKeyName=SharedKey;SharedAccessKey=sHAYVL6e+8TMSSIo0HhFkl1ffQ8YzoIpLsN2OQP0xww=");
    }

    internal static async Task DisposeAsync()
    {
        Console.WriteLine("Cleaning up");

        if (_serviceBusClient != null)
        {
            await _serviceBusClient.DisposeAsync();
            _serviceBusClient = null;
        }
    }
}
