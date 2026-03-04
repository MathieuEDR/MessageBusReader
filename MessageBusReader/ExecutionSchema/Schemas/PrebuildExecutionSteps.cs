using System.Threading;
using MessageBusReader.DataTypes;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.ExecutionSchema.Steps;
using MessageBusReader.Extensions;
using MessageBusReader.Services.Logging;

namespace MessageBusReader.ExecutionSchema.Schemas;

internal static class PrebuildExecutionSteps
{
    public static class Execute
    {
        internal static ConditionAction ReplayAll() => new()
        {
            Condition = MessageFilter.ForAll,
            Action = message => message.ReturnMessageToSourceQueue()
        };

        internal static ConditionAction ReplayMessagesOfType(params string[] targetMessageTypes) => new()
        {
            Condition = message => MessageFilter.OfType(message, targetMessageTypes),
            Action = message => message.ReturnMessageToSourceQueue()
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

        internal static ConditionAction ReturnAllFromDeadLetterQueue(Queue sourceQueue) => new()
        {
            Condition = MessageFilter.ForAll,
            Action = message => MessageAction.ReturnFromDeadLetter(message, sourceQueue)
        };
    }

    public static class Analyze
    {
        internal static ConditionAction ByMessageType() => new()
        {
            Condition = MessageFilter.ForAll,
            Action = message => OperationLogger.CountByMessageType(message.GetMessageType() ?? MessageType.Unknown),
            ExecutionFinishedCallback = OperationLogger.DisplayMessageTypeCount
        };
    }
}
