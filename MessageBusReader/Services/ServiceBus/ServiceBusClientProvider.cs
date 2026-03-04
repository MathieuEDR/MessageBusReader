using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.Services.Logging;
using MessageBusReader.Services.Processors;
using Microsoft.Extensions.Configuration;

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
        
        // Build configuration from user secrets
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<UserTermination>()
            .Build();
        
        // Read connection string from PROD section
        var serviceBusConnectionStringKey = "PROD:ServiceBus:ConnectionString";
        var connectionString = configuration[serviceBusConnectionStringKey];
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"ServiceBus connection string not found in user secrets ({serviceBusConnectionStringKey})");
        }
        
        return new ServiceBusClient(connectionString);
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
