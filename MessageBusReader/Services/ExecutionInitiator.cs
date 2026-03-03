using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;

namespace MessageBusReader.Services;

internal static class ExecutionInitiator
{
    private static TaskCompletionSource<int>? _taskCompletionSource;
    private static Task<int>? _loopTask;


    internal static async Task Start(ExecutionInputConfiguration inputs)
    {
        var client = ServiceBusClientProvider.GetClient();

        var messageProcessor = new MessageProcessor(inputs);

        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1, // ReturnToQueue code does not support concurrent calls yet.
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            SubQueue = inputs.SourceQueue.SubQueue
        };

        var processor = client.CreateProcessor(inputs.SourceQueue.Name.Name, options);

        processor.ProcessMessageAsync += messageProcessor.ProcessMessagesAsync;
        processor.ProcessErrorAsync += messageProcessor.ExceptionReceivedHandler;

        _taskCompletionSource = new TaskCompletionSource<int>();
        _loopTask = _taskCompletionSource.Task;

        await processor.StartProcessingAsync();

        await _loopTask;

        Console.WriteLine("Execution finished");

        Console.ReadLine();
    }
}
