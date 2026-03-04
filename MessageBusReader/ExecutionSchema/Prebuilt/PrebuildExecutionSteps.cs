using System.Threading;
using System.Threading.Tasks;
using MessageBusReader.DataTypes;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.ExecutionSchema.Steps;
using MessageBusReader.Extensions;
using MessageBusReader.Services.Logging;
using Newtonsoft.Json.Linq;

namespace MessageBusReader.ExecutionSchema.Prebuilt;

/// <summary>
/// Provides prebuilt execution steps (ConditionAction instances) that can be composed into custom execution plans.
/// Each step defines a condition for filtering messages and an action to perform on matching messages.
/// </summary>
internal static class PrebuildExecutionSteps
{
    /// <summary>
    /// Execution steps that perform actions on messages (modify, move, delete).
    /// </summary>
    public static class Execute
    {
        /// <summary>
        /// Creates an execution step that returns ALL messages to their original source queue.
        /// </summary>
        /// <returns>A <see cref="ConditionAction"/> that processes all messages.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Matches: ALL messages in the queue (no filtering)</description></item>
        /// <item><description>Reads the 'rbs2-source-queue' application property to determine the original queue</description></item>
        /// <item><description>Creates a copy of each message and sends it to the source queue</description></item>
        /// <item><description>Completes (deletes) each message from the current queue after successful send</description></item>
        /// <item><description>If a message lacks 'rbs2-source-queue' property, logs an error and skips it</description></item>
        /// </list>
        /// <para>Use Case: Return all messages from an error queue back to their originating queues.</para>
        /// <para>Warning: This processes ALL messages without filtering. Use with caution.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var plan = new ExecutionPlan
        /// {
        ///     SourceQueue = new Queue(QueueName.Error.General),
        ///     ExecutionSteps = [PrebuildExecutionSteps.Execute.ReturnAllToSourceQueue()]
        /// };
        /// </code>
        /// </example>
        internal static ConditionAction ReturnAllToSourceQueue() => new()
        {
            Condition = MessageFilter.Include.ForAll,
            Action = message => message.ReturnToSourceQueue()
        };

        /// <summary>
        /// Creates an execution step that returns only messages of specific types to their original source queue.
        /// </summary>
        /// <param name="targetMessageTypes">Fully-qualified message type names to filter for (e.g., "MyApp.Events.OrderCreated, MyApp.Contracts").</param>
        /// <returns>A <see cref="ConditionAction"/> that processes only matching message types.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Matches: Only messages where 'rbs2-msg-type' matches one of the specified types</description></item>
        /// <item><description>For matching messages: reads 'rbs2-source-queue' to determine the original queue</description></item>
        /// <item><description>Creates a copy of matching messages and sends them to their source queue</description></item>
        /// <item><description>Completes (deletes) matching messages from the current queue</description></item>
        /// <item><description>Non-matching messages are left untouched</description></item>
        /// </list>
        /// <para>Use Case: Selective replay of specific message types from an error queue.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var step = PrebuildExecutionSteps.Execute.ReturnMessagesOfType(
        ///     "MyApp.Events.OrderCreated, MyApp.Contracts",
        ///     "MyApp.Events.OrderUpdated, MyApp.Contracts"
        /// );
        /// </code>
        /// </example>
        internal static ConditionAction ReturnMessagesOfType(params string[] targetMessageTypes) => new()
        {
            Condition = message => MessageFilter.Include.OfType(message, targetMessageTypes),
            Action = message => message.ReturnToSourceQueue()
        };

        /// <summary>
        /// Creates an execution step that permanently deletes messages of specific types from the queue.
        /// </summary>
        /// <param name="targetMessageTypes">Fully-qualified message type names to delete (e.g., "MyApp.Events.ObsoleteEvent, MyApp.Contracts").</param>
        /// <returns>A <see cref="ConditionAction"/> that deletes only matching message types.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Matches: Only messages where 'rbs2-msg-type' matches one of the specified types</description></item>
        /// <item><description>Completes (permanently deletes) matching messages from the queue</description></item>
        /// <item><description>Non-matching messages are left untouched</description></item>
        /// <item><description>Deleted messages CANNOT be recovered</description></item>
        /// </list>
        /// <para>⚠️ DANGER: This is a destructive operation. Deleted messages cannot be recovered.</para>
        /// <para>Use Case: Remove obsolete, test, or unwanted message types from a queue.</para>
        /// <para>Recommendation: Test filters on a small dataset first, or use CollectAndOutput.CountByMessageType() 
        /// to verify what will be deleted.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var step = PrebuildExecutionSteps.Execute.DeleteMessagesOfType(
        ///     "MyApp.Events.TestEvent, MyApp.Contracts"
        /// );
        /// </code>
        /// </example>
        internal static ConditionAction DeleteMessagesOfType(params string[] targetMessageTypes) => new()
        {
            Condition = message => MessageFilter.Include.OfType(message, targetMessageTypes),
            Action = message => message.CompleteMessageAsync(message.Message, CancellationToken.None),
        };

        /// <summary>
        /// Creates an execution step that permanently deletes ALL messages from the queue.
        /// </summary>
        /// <returns>A <see cref="ConditionAction"/> that deletes all messages.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Matches: ALL messages in the queue (no filtering)</description></item>
        /// <item><description>Completes (permanently deletes) every message</description></item>
        /// <item><description>Deleted messages CANNOT be recovered</description></item>
        /// <item><description>The queue itself is not deleted, only its contents</description></item>
        /// </list>
        /// <para>⚠️⚠️ EXTREME DANGER ⚠️⚠️: This is a highly destructive operation that deletes ALL messages 
        /// without any filtering. Use with extreme caution.</para>
        /// <para>Use Case: Purging a test queue, or clearing a queue that's known to contain only invalid data.</para>
        /// <para>Recommendation: Only use in non-production environments or after explicit confirmation.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Only use this if you're absolutely certain!
        /// var step = PrebuildExecutionSteps.Execute.DeleteAll();
        /// </code>
        /// </example>
        internal static ConditionAction DeleteAll() => new()
        {
            Condition = MessageFilter.Include.ForAll,
            Action = message => message.CompleteMessageAsync(message.Message, CancellationToken.None),
        };

        /// <summary>
        /// Creates an execution step that sends ALL messages to a specified target queue.
        /// </summary>
        /// <param name="sourceQueue">The target queue to send messages to.</param>
        /// <returns>A <see cref="ConditionAction"/> that sends all messages to the specified queue.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Matches: ALL messages in the queue (no filtering)</description></item>
        /// <item><description>Creates a copy of each message and sends it to the target queue</description></item>
        /// <item><description>Completes (deletes) each message from the source queue after successful send</description></item>
        /// <item><description>Messages appear in the target queue as new messages</description></item>
        /// <item><description>Original message properties and body are preserved</description></item>
        /// </list>
        /// <para>Use Case: Move/migrate all messages from one queue to another, or archive messages to a different queue.</para>
        /// <para>Note: This is effectively a move operation (copy + delete). Messages are removed from the source queue.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var targetQueue = new Queue(new QueueName("archive-queue"));
        /// var step = PrebuildExecutionSteps.Execute.SendAllToQueue(targetQueue);
        /// </code>
        /// </example>
        internal static ConditionAction SendAllToQueue(Queue sourceQueue) => new()
        {
            Condition = MessageFilter.Include.ForAll,
            Action = message => MessageAction.SendToQueue(message, sourceQueue)
        };
    }

    /// <summary>
    /// Execution steps that collect data from messages for analysis without modifying or deleting them.
    /// Results are output via callback when execution completes.
    /// </summary>
    public static class CollectAndOutput
    {
        private static readonly Logger Logger = new(nameof(CollectAndOutput));

        /// <summary>
        /// Creates an execution step that counts all messages grouped by message type and outputs the results.
        /// </summary>
        /// <returns>A <see cref="ConditionAction"/> with a callback that displays the count summary.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Matches: ALL messages in the queue</description></item>
        /// <item><description>Reads only the 'rbs2-msg-type' application property from each message</description></item>
        /// <item><description>Messages are NOT modified, moved, or deleted</description></item>
        /// <item><description>Accumulates counts in memory during processing</description></item>
        /// <item><description>Outputs a summary report via ExecutionFinishedCallback when all messages are processed</description></item>
        /// </list>
        /// <para>Safe Operation: This is completely non-destructive and read-only.</para>
        /// <para>Use Case: Analyze message type distribution before performing operations, or for monitoring/reporting.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var plan = new ExecutionPlan
        /// {
        ///     SourceQueue = new Queue(QueueName.Error.General),
        ///     ExecutionSteps = [PrebuildExecutionSteps.CollectAndOutput.CountByMessageType()]
        /// };
        /// // After execution completes, output will show: MessageType1: 42, MessageType2: 15, etc.
        /// </code>
        /// </example>
        internal static ConditionAction CountByMessageType() => new()
        {
            Condition = MessageFilter.Include.ForAll,
            Action = message => OperationLogger.CountByMessageType(ProcessMessageEventArgsExtensions.GetType(message) ?? MessageType.Unknown),
            ExecutionFinishedCallback = OperationLogger.DisplayMessageTypeCount
        };

        /// <summary>
        /// Creates an execution step that extracts a specific data point from message bodies using JSONPath and outputs all collected values.
        /// </summary>
        /// <param name="dataPointPath">JSONPath expression to extract data (e.g., "$.Order.OrderNumber", "$.Customer.Email").</param>
        /// <param name="targetMessageTypes">Fully-qualified message type names to process (e.g., "MyApp.Events.OrderCreated, MyApp.Contracts").</param>
        /// <returns>A <see cref="ConditionAction"/> with a callback that outputs all collected data points.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Matches: Only messages where 'rbs2-msg-type' matches one of the specified types</description></item>
        /// <item><description>Deserializes the message body as JSON</description></item>
        /// <item><description>Extracts the value at the specified JSONPath</description></item>
        /// <item><description>Messages are NOT modified, moved, or deleted</description></item>
        /// <item><description>Collects all extracted values in memory</description></item>
        /// <item><description>Outputs all collected values via ExecutionFinishedCallback when processing completes</description></item>
        /// <item><description>Logs a warning if the JSONPath doesn't match or the value is null</description></item>
        /// </list>
        /// <para>Safe Operation: This is completely non-destructive and read-only.</para>
        /// <para>Use Case: Extract specific fields (order IDs, customer emails, etc.) from messages for analysis, 
        /// reporting, or building a list for batch operations.</para>
        /// <para>JSONPath Examples:</para>
        /// <list type="bullet">
        /// <item><description>"$.OrderId" - Direct property</description></item>
        /// <item><description>"$.Order.OrderNumber" - Nested property</description></item>
        /// <item><description>"$.Customer.Email" - Another nested property</description></item>
        /// </list>
        /// </remarks>
        /// <example>
        /// <code>
        /// var step = PrebuildExecutionSteps.CollectAndOutput.DataPointFromBodyForMessageType(
        ///     "$.Order.OrderNumber",
        ///     "MyApp.Events.OrderCreated, MyApp.Contracts"
        /// );
        /// // After execution: outputs list of all order numbers found
        /// </code>
        /// </example>
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
