using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.Services.Logging;

namespace MessageBusReader.Services.Processors;

internal class MessageProcessor(ExecutionPlan inputs)
{
    private static readonly Logger Logger = new(nameof(QueueProcessor));

    internal async Task ProcessMessagesAsync(ProcessMessageEventArgs messageEvent)
    {
        OperationLogger.MessageProcessingStarted(messageEvent);

        foreach (var inputsExecutionStep in inputs.ExecutionSteps.Where(inputsExecutionStep => inputsExecutionStep.Condition(messageEvent)))
        {
            Logger.Log("Executing action");
            await inputsExecutionStep.Action(messageEvent);
        }

        OperationLogger.MessageProcessingFinished(messageEvent);
    }

    internal Task ExceptionReceivedHandler(ProcessErrorEventArgs messageEvent)
    {
        Logger.LogError($"Message handler encountered an exception {messageEvent.Exception}.");
        return Task.CompletedTask;
    }
}
