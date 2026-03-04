using System.Threading;
using MessageBusReader.DataTypes;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.ExecutionSchema.Steps;
using MessageBusReader.Extensions;
using MessageBusReader.Services.Logging;

namespace MessageBusReader.ExecutionSchema.Prebuilt;

internal static class PrebuildExecutionSteps
{
    public static class Execute
    {
        internal static ConditionAction ReplayAll() => new()
        {
            Condition = MessageFilter.ForAll,
            Action = message => message.ReturnToSourceQueue()
        };

        internal static ConditionAction ReplayMessagesOfType(params string[] targetMessageTypes) => new()
        {
            Condition = message => MessageFilter.OfType(message, targetMessageTypes),
            Action = message => message.ReturnToSourceQueue()
        };

        internal static ConditionAction DeleteMessagesOfType(params string[] targetMessageTypes) => new()
        {
            Condition = message => MessageFilter.OfType(message, targetMessageTypes),
            Action = message => message.CompleteMessageAsync(message.Message, CancellationToken.None),
        };

        internal static ConditionAction DeleteAll() => new()
        {
            Condition = MessageFilter.ForAll,
            Action = message => message.CompleteMessageAsync(message.Message, CancellationToken.None),
        };

        internal static ConditionAction SendAllToQueue(Queue sourceQueue) => new()
        {
            Condition = MessageFilter.ForAll,
            Action = message => MessageAction.SendToQueue(message, sourceQueue)
        };
    }

    public static class Analyze
    {
        internal static ConditionAction ByMessageType() => new()
        {
            Condition = MessageFilter.ForAll,
            Action = message => OperationLogger.CountByMessageType(ProcessMessageEventArgsExtensions.GetType(message) ?? MessageType.Unknown),
            ExecutionFinishedCallback = OperationLogger.DisplayMessageTypeCount
        };
    }
}
