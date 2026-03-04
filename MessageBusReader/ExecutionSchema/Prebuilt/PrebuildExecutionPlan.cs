using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.Services.Logging;

namespace MessageBusReader.ExecutionSchema.Prebuilt;

internal static class PrebuildExecutionPlan
{
    internal static class Execute
    {
        internal static ExecutionPlan ReturnAllFromDeadLetter(QueueName sourceQueueName) => new()
        {
            SourceQueue = new Queue(sourceQueueName, SubQueue.DeadLetter),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Execute.SendAllToQueue(new Queue(sourceQueueName))
            ]
        };

        public static ExecutionPlan ReplayMessagedOfType(QueueName sourceQueueName, params string[] targetMessageTypes) => new()
        {
            SourceQueue = new Queue(sourceQueueName),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Execute.ReturnMessagesOfType(targetMessageTypes)
            ]
        };
    }


    internal static class CollectAndOutput
    {
        private static readonly Logger Logger = new(nameof(CollectAndOutput));

        internal static ExecutionPlan CountByMessageType(QueueName sourceQueueName) => new()
        {
            SourceQueue = new Queue(sourceQueueName),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.CollectAndOutput.CountByMessageType()
            ]
        };

        internal static ExecutionPlan OrderNumberFromOrderRefreshFromShopDownloadedV2(QueueName sourceQueueName) => new()
        {
            SourceQueue = new Queue(sourceQueueName),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.CollectAndOutput.DataPointFromBodyForMessageType(
                    "$.Order.OrderNumber",
                    "Edrington.Contracts.Orders.Events.OrderRefreshFromShopDownloadedV2, Edrington.Contracts.Orders"),
            ]
        };
    }
}
