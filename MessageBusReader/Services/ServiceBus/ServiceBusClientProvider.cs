using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.Services.Logging;
using MessageBusReader.Services.Processors;

namespace MessageBusReader.Services.ServiceBus;

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
        return new ServiceBusClient("");
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
