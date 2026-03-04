using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.DataTypes.Queue;

namespace MessageBusReader.ExecutionSchema.Schemas;

internal static class PrebuildConfigurations
{
    internal static class Execute
    {
        internal static ExecutionInputConfiguration ReturnAllFromDeadLetter(QueueName queueName) => new()
        {
            SourceQueue = new Queue(queueName, SubQueue.DeadLetter),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Execute.SendAllToQueue(new Queue(queueName))
            ]
        };
    }

        
        internal static ExecutionInputConfiguration Analyze(QueueName queueName) => new()
        {
            SourceQueue = new Queue(queueName),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Analyze.ByMessageType()
            ]
        };
    
}
