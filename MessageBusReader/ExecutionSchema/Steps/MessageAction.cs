using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.DataTypes;
using MessageBusReader.Extensions;
using MessageBusReader.Services;

namespace MessageBusReader.ExecutionSchema.Steps;

internal static class MessageAction
{
    public static async Task ReturnFromDeadLetter(ProcessMessageEventArgs message, SourceQueue sourceQueue)
    {
        OperationLogger.MessageProcessingStarted(message);

        var clone = new ServiceBusMessage(message.Message);

        await clone.ReturnMessageToQueue(new TargetQueue(sourceQueue.Name));
        await message.CompleteMessage();
    }
}
