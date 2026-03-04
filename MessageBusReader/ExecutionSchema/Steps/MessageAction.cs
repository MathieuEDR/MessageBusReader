using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.Extensions;
using MessageBusReader.Services.Logging;

namespace MessageBusReader.ExecutionSchema.Steps;

internal static class MessageAction
{
    public static async Task ReturnFromDeadLetter(ProcessMessageEventArgs message, Queue sourceQueue)
    {
        OperationLogger.MessageProcessingStarted(message);

        var clone = new ServiceBusMessage(message.Message);

        await clone.ReturnMessageToQueue(new Queue(sourceQueue.Name));
        await message.CompleteMessage();
    }
}
