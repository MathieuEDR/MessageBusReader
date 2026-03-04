using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.DataTypes.Queue;

namespace MessageBusReader.ExecutionSchema.Prebuilt;

internal static class PrebuildExecutionPlan
{
    internal static class Execute
    {
        internal static ExecutionPlan ReturnAllFromDeadLetter(QueueName queueName) => new()
        {
            SourceQueue = new Queue(queueName, SubQueue.DeadLetter),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Execute.SendAllToQueue(new Queue(queueName))
            ]
        };
    }

    internal static class Analyze
    {
        internal static ExecutionPlan ByMessageType(QueueName queueName) => new()
        {
            SourceQueue = new Queue(queueName),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Analyze.ByMessageType()
            ]
        };
    }
}
