using System.Collections.Generic;
using MessageBusReader.Configuration;
using MessageBusReader.DataTypes;
using MessageBusReader.Services;

namespace MessageBusReader;

using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

internal static class Program
{
    private static TaskCompletionSource<int> _taskCompletionSource;
    private static Task<int> _loopTask;

    static async Task Main(string[] args)
    {
        var serviceBusClientBuilder = new ServiceBusClientBuilder();
        var client = serviceBusClientBuilder.BuildServiceBusClient();
        var inputs = new ExecutionInputConfiguration()
        {
            TargetErrorQueue = ErrorQueue.General,
            MessageTypesToDeleteWithoutAction =
            [
                // "Edrington.Contracts.Ecommerce.Events.CheckoutCompleted, Edrington.Contracts.Ecommerce"
            ],
            MessageTypesToReplay =
            [
                // "Edrington.Contracts.Orders.Events.OrderRefreshFromShopDownloadedV2, Edrington.Contracts.Orders"
                // "Edrington.Contracts.Ecommerce.Events.CheckoutCompleted, Edrington.Contracts.Ecommerce",
                "Edrington.Data.Consumer.Tiering.SetTieringModel, Edrington.Data",
                "Edrington.Data.Consumer.Tiering.SetConsumerTierAtCrm, Edrington.Data"
            ],
            MessagesToCollectDataFrom =
            [
                // "Edrington.Contracts.Ecommerce.Events.CheckoutCompleted, Edrington.Contracts.Ecommerce"
            ],
            DeadLetterEverythingElse = false,
            CollectCountByMessageType = false,
        };

        List<COnditionAction> ExecutionConfig =
        [
            new COnditionAction()
            {
                Condition = message => true,
                Action = () => Console.WriteLine("action")
            }
        ];

        var messageProcessor = new MessageProcessor(new ServiceBusSenderCache(client), inputs);

        await ProcessMessages(inputs.TargetErrorQueue, messageProcessor, client);

        // Switch to this to move deadletter back to the error queue
        // await ReturnToDeadLetter(inputs.TargetErrorQueue, messageProcessor, client);

        Console.WriteLine("Execution finished");

        Console.ReadLine();
    }


    static async Task ProcessMessages(ErrorQueue queueName, MessageProcessor messageProcessor, ServiceBusClient client)
    {
        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1,
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
        };

        var processor = client.CreateProcessor(queueName.GetQueueName(), options);


        processor.ProcessMessageAsync += messageProcessor.ProcessMessagesAsync;
        processor.ProcessErrorAsync += messageProcessor.ExceptionReceivedHandler;

        _taskCompletionSource = new TaskCompletionSource<int>();
        _loopTask = _taskCompletionSource.Task;

        await processor.StartProcessingAsync();

        await _loopTask;
    }

    static async Task ReturnToDeadLetter(ErrorQueue queueName, MessageProcessor messageProcessor, ServiceBusClient client)
    {
        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1, // ReturnToQueue code does not support concurrent calls yet.
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
            SubQueue = SubQueue.DeadLetter
        };

        var processor = client.CreateProcessor(queueName.GetQueueName(), options);

        processor.ProcessMessageAsync += messageEvent => messageProcessor.MoveAllDeadLetterMessageBackToMainQueue(messageEvent, queueName);
        processor.ProcessErrorAsync += messageProcessor
            .ExceptionReceivedHandler;

        _taskCompletionSource = new TaskCompletionSource<int>();
        _loopTask = _taskCompletionSource.Task;

        await processor.StartProcessingAsync();

        await _loopTask;
    }
}

internal class COnditionAction
{
    public Func<ServiceBusMessage, bool> Condition { get; set; }
    public Action Action { get; set; }
}

internal class ServiceBusClientBuilder
{
    internal ServiceBusClient BuildServiceBusClient()
    {
        return new ServiceBusClient("Endpoint=sb://prodedringtonservicebus.servicebus.windows.net/;SharedAccessKeyName=SharedKey;SharedAccessKey=sHAYVL6e+8TMSSIo0HhFkl1ffQ8YzoIpLsN2OQP0xww=");
    }
}
