using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.Services.Logging;
using MessageBusReader.Services.ServiceBus;

namespace MessageBusReader.Services.Processors;

internal class QueueProcessor : IAsyncDisposable
{
    private readonly ServiceBusProcessor _processor;
    private static readonly Logger Logger = new(nameof(QueueProcessor));

    public QueueProcessor(ExecutionInputConfiguration inputs)
    {
        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1, // ReturnToQueue code does not support concurrent calls yet.
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            SubQueue = inputs.SourceQueue.SubQueue
        };
        
        var client = ServiceBusClientProvider.GetClient();

        Logger.Log("Creating");
        _processor = client.CreateProcessor(inputs.SourceQueue.Name.Name, options);

        var messageProcessor = new MessageProcessor(inputs);

        Logger.Log("Registering callbacks");
        _processor.ProcessMessageAsync += messageProcessor.ProcessMessagesAsync;
        _processor.ProcessErrorAsync += messageProcessor.ExceptionReceivedHandler;
    }


    public async Task Start()
    {
        Logger.Log("Starting");
        await _processor.StartProcessingAsync();
    }

    public async Task Stop()
    {
        Logger.Log("Stoping");
        await _processor.StopProcessingAsync();
    }

    public async ValueTask DisposeAsync()
    {
        Logger.Log("Disposing");
        await _processor.DisposeAsync();
    }
}