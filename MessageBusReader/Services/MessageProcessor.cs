using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.DataTypes;
using MessageBusReader.Extensions;
using Newtonsoft.Json.Linq;

namespace MessageBusReader.Services;

internal class MessageProcessor(ServiceBusSenderCache serviceBusSenderCache, ExecutionInputConfiguration inputs)
{
    private ServiceBusSenderCache ServiceBusSenderCache { get; } = serviceBusSenderCache;

    private int _delayInSeconds;

    internal async Task ProcessMessagesAsync(ProcessMessageEventArgs messageEvent)
    {
        await ThrowIfMessageDeserializationFails(messageEvent, messageEvent.Message, LogFilePathProvider.ErrorFilename);

        OperationLogger.RecordMessageProcessing(messageEvent);

        var messageType = messageEvent.GetMessageType();
        if (messageType == null)
        {
            OperationLogger.Warning("Skipping message as unable to get message type");
            return;
        }

        if (inputs.MessageTypesToDeleteWithoutAction.Contains(messageType))
        {
            await CompleteMessage(messageEvent);
            return;
        }

        if (inputs.MessageTypesToReplay.Contains(messageType))
        {
            await ReturnMessageToSourceQueue(messageEvent, _delayInSeconds);
            _delayInSeconds += 5;
            return;
        }

        if (inputs.CollectCountByMessageType)
        {
            await CollectCountByMessageType(messageEvent, _delayInSeconds);
            return;
        }

        if (inputs.MessagesToCollectDataFrom.Contains(messageType))
        {
            await CollectData(messageEvent);
            return;
        }

        // And we move anything else to DLQ
        if (inputs.DeadLetterEverythingElse)
        {
            await messageEvent.DeadLetterMessageAsync(messageEvent.Message);
        }
    }

    private static async Task ThrowIfMessageDeserializationFails(ProcessMessageEventArgs messageEvent, ServiceBusReceivedMessage message, string errorFilename)
    {
        try
        {
            JObject.Parse(Encoding.UTF8.GetString(message.Body));
        }
        catch (Exception)
        {
            await File.AppendAllTextAsync(errorFilename, messageEvent.Message.Subject);
            await File.AppendAllTextAsync(errorFilename, "\n");
            throw;
        }
    }

    internal async Task MoveAllDeadLetterMessageBackToMainQueue(ProcessMessageEventArgs messageEvent, ErrorQueue queueName)
    {
        OperationLogger.RecordMessageProcessing(messageEvent);


        var clone = new ServiceBusMessage(messageEvent.Message);

        await ReturnMessageToQueue(clone, queueName.GetQueueName());
        await CompleteMessage(messageEvent);
    }

    private async Task CompleteMessage(ProcessMessageEventArgs messageEvent)
    {
        await messageEvent.CompleteMessageAsync(messageEvent.Message);

        OperationLogger.MessageCompleted();
    }

    private async Task CollectCountByMessageType(ProcessMessageEventArgs messageEvent, int delayInSeconds = 0)
    {
        OperationLogger.CountByMessageType(messageEvent.GetMessageType());
    }

    private async Task CollectData(ProcessMessageEventArgs messageEvent, int delayInSeconds = 0)
    {
        var body = JObject.Parse(Encoding.UTF8.GetString(messageEvent.Message.Body));

        foreach (var orderNumber in body["OrderIds"]?["$values"]?.ToList() ?? new List<JToken>())
        {
            OperationLogger.CollectDataPoint(orderNumber.ToString());

        }
    }

    private async Task ReturnMessageToSourceQueue(ProcessMessageEventArgs messageEvent, int delayInSeconds = 0)
    {
        var source = messageEvent.GetMessageSourceQueue();

        if (source == null)
        {
            OperationLogger.Error("Message does not have a source queue property");
            return;
        }

        var copy = new ServiceBusMessage(messageEvent.Message);

        await ReturnMessageToQueue(copy, source, delayInSeconds);

        await CompleteMessage(messageEvent);
    }


    private async Task ReturnMessageToQueue(ServiceBusMessage message, string queueName, int delayInSeconds = 0)
    {
        var sender = ServiceBusSenderCache.GetSender(queueName);

        if (delayInSeconds > 0)
        {
            await sender.ScheduleMessageAsync(message, DateTimeOffset.UtcNow.AddSeconds(delayInSeconds));
        }
        else
        {
            await sender.SendMessageAsync(message);
        }

        OperationLogger.MessageReturned(queueName);
    }


    internal Task ExceptionReceivedHandler(ProcessErrorEventArgs messageEvent)
    {
        OperationLogger.Error($"Message handler encountered an exception {messageEvent.Exception}.");
        return Task.CompletedTask;
    }
}
