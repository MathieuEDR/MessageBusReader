using System;
using System.Linq;
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

        Console.WriteLine("Building message processor");
        var messageProcessor = new MessageProcessor(inputs);

        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1, // ReturnToQueue code does not support concurrent calls yet.
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            SubQueue = inputs.SourceQueue.SubQueue
        };

        Console.WriteLine("Creating processor");
        var processor = client.CreateProcessor(inputs.SourceQueue.Name.Name, options);

        Console.WriteLine("Registering processor callbacks");
        processor.ProcessMessageAsync += messageProcessor.ProcessMessagesAsync;
        processor.ProcessErrorAsync += messageProcessor.ExceptionReceivedHandler;

        _taskCompletionSource = new TaskCompletionSource<int>();
        _loopTask = _taskCompletionSource.Task;

        Console.WriteLine("Starting processor");
        await processor.StartProcessingAsync();

        Console.WriteLine($"Is Processing: {processor.IsProcessing}");
        Console.WriteLine($"Is Closed: {processor.IsClosed}");
        await _loopTask;

        Console.WriteLine("Execution finished");
        Console.ReadLine();
        
        Console.WriteLine("Stoping processor");
        await processor.StopProcessingAsync();
        await ExecuteCallbacks(inputs);

    }

    private static async Task ExecuteCallbacks(ExecutionInputConfiguration inputs)
    {
        Console.WriteLine("Executing callbacks");
        foreach (var executionStep in inputs.ExecutionSteps)
        {
            if (executionStep.ExecutionFinishedCallback != null)
            {
                await executionStep.ExecutionFinishedCallback();
            }
        }
    }
}
