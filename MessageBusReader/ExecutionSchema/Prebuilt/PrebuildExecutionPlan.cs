using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.Services.Logging;

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

    internal static class CollectAndOutput
    {
        private static readonly Logger Logger = new(nameof(CollectAndOutput));

        internal static ExecutionPlan OrderNumberFromOrderRefreshFromShopDownloadedV2(QueueName queueName) => new()
        {
            SourceQueue = new Queue(queueName),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.CollectAndOutput.DataPointFromBodyForMessageType(
                    "$.Order.OrderNumber",
                    "Edrington.Contracts.Orders.Events.OrderRefreshFromShopDownloadedV2, Edrington.Contracts.Orders"),
            ]
        };
    }
}
