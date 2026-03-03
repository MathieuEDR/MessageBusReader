using System.Threading;
using MessageBusReader.DataTypes;
using MessageBusReader.ExecutionSchema.Steps;
using MessageBusReader.Extensions;
using MessageBusReader.Services;

namespace MessageBusReader.ExecutionSchema.Schemas;

internal static class PrebuildExecutionSteps
{
    public static class WithSideEffect
    {
        internal static ConditionAction ReturnMessagedOfTypeToSourceQueue(params string[] targetMessageTypes) => new()
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

        internal static ConditionAction ReturnAllFromDeadLetterQueue(SourceQueue sourceQueue) => new()
        {
            Condition = MessageFilter.ForAll,
            Action = message => MessageAction.ReturnFromDeadLetter(message, sourceQueue)
        };
    }

    public static class Analysis
    {
        internal static ConditionAction AnalyzeMessagesByType() => new()
        {
            Condition = MessageFilter.ForAll,
            Action = message => OperationLogger.CountByMessageType(message.GetMessageType() ?? MessageType.Unknown),
            ExecutionFinishedCallback = OperationLogger.DisplayMessageTypeCount
        };
    }
}
