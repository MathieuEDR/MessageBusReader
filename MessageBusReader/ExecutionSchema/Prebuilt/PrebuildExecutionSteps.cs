using System.Threading;
using System.Threading.Tasks;
using MessageBusReader.DataTypes;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.ExecutionSchema.Steps;
using MessageBusReader.Extensions;
using MessageBusReader.Services.Logging;
using Newtonsoft.Json.Linq;

namespace MessageBusReader.ExecutionSchema.Prebuilt;

internal static class PrebuildExecutionSteps
{
    public static class Execute
    {
        internal static ConditionAction ReturnAllToSourceQueue() => new()
        {
            Condition = MessageFilter.Include.ForAll,
            Action = message => message.ReturnToSourceQueue()
        };

        internal static ConditionAction ReturnMessagesOfType(params string[] targetMessageTypes) => new()
        {
            Condition = message => MessageFilter.Include.OfType(message, targetMessageTypes),
            Action = message => message.ReturnToSourceQueue()
        };

        internal static ConditionAction DeleteMessagesOfType(params string[] targetMessageTypes) => new()
        {
            Condition = message => MessageFilter.Include.OfType(message, targetMessageTypes),
            Action = message => message.CompleteMessageAsync(message.Message, CancellationToken.None),
        };

        internal static ConditionAction DeleteAll() => new()
        {
            Condition = MessageFilter.Include.ForAll,
            Action = message => message.CompleteMessageAsync(message.Message, CancellationToken.None),
        };

        internal static ConditionAction SendAllToQueue(Queue sourceQueue) => new()
        {
            Condition = MessageFilter.Include.ForAll,
            Action = message => MessageAction.SendToQueue(message, sourceQueue)
        };
    }

    public static class CollectAndOutput
    {
        private static readonly Logger Logger = new(nameof(CollectAndOutput));

        internal static ConditionAction CountByMessageType() => new()
        {
            Condition = MessageFilter.Include.ForAll,
            Action = message => OperationLogger.CountByMessageType(ProcessMessageEventArgsExtensions.GetType(message) ?? MessageType.Unknown),
            ExecutionFinishedCallback = OperationLogger.DisplayMessageTypeCount
        };

        internal static ConditionAction DataPointFromBodyForMessageType(string dataPointPath, params string[] targetMessageTypes) => new()
        {
            Condition = message => MessageFilter.Include.OfType(message, targetMessageTypes),
            Action = message =>
            {
                var obj = JObject.Parse(message.Deserialize());

                var dataPoint = obj.SelectToken(dataPointPath)?.Value<string>();

                if (dataPoint is null)
                {
                    Logger.LogWarning($"Could not get datapoint {dataPointPath} from message{message.Message.MessageId}");
                    return Task.CompletedTask;
                }

                MessageDataLogger.CollectDataPoint(dataPoint);
                Logger.DataPointCollected(message, dataPoint);


                return Task.CompletedTask;
            },
            ExecutionFinishedCallback = MessageDataLogger.OutputCollectedData
        };
    }
}
