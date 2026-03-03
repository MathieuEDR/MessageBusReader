using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.Extensions;

namespace MessageBusReader.Services;

internal class MessageProcessor(ExecutionInputConfiguration inputs)
{
    internal async Task ProcessMessagesAsync(ProcessMessageEventArgs messageEvent)
    {
        OperationLogger.RecordMessageProcessing(messageEvent);

        var messageType = messageEvent.GetMessageType();
        if (messageType == null)
        {
            OperationLogger.Warning("Skipping message as unable to get message type");
            return;
        }

        foreach (var inputsExecutionStep in inputs.ExecutionSteps)
        {
            if (inputsExecutionStep.Condition(messageEvent))
            {
                await inputsExecutionStep.Action(messageEvent);
            }
        }
    }

    internal Task ExceptionReceivedHandler(ProcessErrorEventArgs messageEvent)
    {
        OperationLogger.Error($"Message handler encountered an exception {messageEvent.Exception}.");
        return Task.CompletedTask;
    }
}
