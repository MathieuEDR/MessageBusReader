using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.Services.Logging;

namespace MessageBusReader.ExecutionSchema.Prebuilt;

/// <summary>
/// Provides prebuilt, ready-to-use execution plans for common Service Bus message processing scenarios.
/// These plans combine source queue configuration with one or more execution steps.
/// </summary>
internal static class PrebuildExecutionPlan
{
    /// <summary>
    /// Execution plans that perform actions on messages (modify, move, delete).
    /// </summary>
    internal static class Execute
    {
        /// <summary>
        /// Creates an execution plan that retrieves all messages from a dead-letter queue and sends them back to the main queue.
        /// </summary>
        /// <param name="sourceQueueName">The name of the queue whose dead-letter sub-queue will be processed.</param>
        /// <returns>An execution plan configured to move all messages from the dead-letter queue back to the main error queue.</returns>
        /// <remarks>
        /// <b>Service Bus Effects:</b>
        /// <list type="bullet">
        /// <item><description>Reads all messages from the specified queue's dead-letter sub-queue</description></item>
        /// <item><description>Creates a copy of each message and sends it to the main queue</description></item>
        /// <item><description>Completes (deletes) each message from the dead-letter queue after successful send</description></item>
        /// </list>
        /// <para>Use Case: Recovery from dead-lettered messages after fixing the underlying issue that caused them to fail.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var plan = PrebuildExecutionPlan.Execute.ReturnAllFromDeadLetter(QueueName.Error.Order);
        /// await StartProgramExecution(plan);
        /// </code>
        /// </example>
        internal static ExecutionPlan ReturnAllFromDeadLetter(QueueName sourceQueueName) => new()
        {
            SourceQueue = new Queue(sourceQueueName, SubQueue.DeadLetter),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Execute.SendAllToQueue(new Queue(sourceQueueName))
            ]
        };

        /// <summary>
        /// Creates an execution plan that filters messages by type and returns matching messages to their original source queue.
        /// </summary>
        /// <param name="sourceQueueName">The name of the queue to process.</param>
        /// <param name="targetMessageTypes">One or more fully-qualified message type names to filter for (e.g., "MyApp.Events.OrderCreated, MyApp.Contracts").</param>
        /// <returns>An execution plan configured to replay specific message types.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Reads all messages from the specified queue</description></item>
        /// <item><description>Filters messages by checking the 'rbs2-msg-type' application property</description></item>
        /// <item><description>For matching messages: reads the 'rbs2-source-queue' property to determine the original queue</description></item>
        /// <item><description>Sends matching messages to their source queue</description></item>
        /// <item><description>Completes (deletes) matching messages from the current queue</description></item>
        /// <item><description>Non-matching messages are left untouched in the queue</description></item>
        /// </list>
        /// <para>Important: Messages must have 'rbs2-source-queue' property set to be returned successfully.</para>
        /// <para>Use Case: Selective replay of specific message types from an error queue back to their originating queue.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var plan = PrebuildExecutionPlan.Execute.ReplayMessagedOfType(
        ///     QueueName.Error.General,
        ///     "MyApp.Events.OrderCreated, MyApp.Contracts",
        ///     "MyApp.Events.OrderUpdated, MyApp.Contracts"
        /// );
        /// await StartProgramExecution(plan);
        /// </code>
        /// </example>
        public static ExecutionPlan ReplayMessagedOfType(QueueName sourceQueueName, params string[] targetMessageTypes) => new()
        {
            SourceQueue = new Queue(sourceQueueName),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Execute.ReturnMessagesOfType(targetMessageTypes)
            ]
        };
    }

    /// <summary>
    /// Execution plans that collect data from messages without modifying or deleting them.
    /// Results are output when execution completes.
    /// </summary>
    internal static class CollectAndOutput
    {
        private static readonly Logger Logger = new(nameof(CollectAndOutput));

        /// <summary>
        /// Creates an execution plan that counts all messages in a queue grouped by message type and outputs the results.
        /// </summary>
        /// <param name="sourceQueueName">The name of the queue to analyze.</param>
        /// <returns>An execution plan configured to count and report message types.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Reads all messages from the specified queue</description></item>
        /// <item><description>Messages are NOT modified, moved, or deleted</description></item>
        /// <item><description>Only reads the 'rbs2-msg-type' application property from each message</description></item>
        /// <item><description>After processing all messages, outputs a summary count by message type</description></item>
        /// </list>
        /// <para>Use Case: Non-destructive analysis to understand message type distribution in a queue.</para>
        /// <para>Safe Operation: This is a read-only operation that does not affect messages.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var plan = PrebuildExecutionPlan.CollectAndOutput.CountByMessageType(QueueName.Error.General);
        /// await StartProgramExecution(plan);
        /// // Output:
        /// // MessageType1: 42
        /// // MessageType2: 15
        /// // [...]
        /// </code>
        /// </example>
        internal static ExecutionPlan CountByMessageType(QueueName sourceQueueName) => new()
        {
            SourceQueue = new Queue(sourceQueueName),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.CollectAndOutput.CountByMessageType()
            ]
        };

        /// <summary>
        /// Creates an execution plan that extracts order numbers from OrderRefreshFromShopDownloadedV2 messages and outputs them.
        /// </summary>
        /// <param name="sourceQueueName">The name of the queue to process.</param>
        /// <returns>An execution plan configured to extract and output order numbers.</returns>
        /// <remarks>
        /// Service Bus Effects:
        /// <list type="bullet">
        /// <item><description>Reads all messages from the specified queue</description></item>
        /// <item><description>Filters for "Edrington.Contracts.Orders.Events.OrderRefreshFromShopDownloadedV2" message type</description></item>
        /// <item><description>Deserializes matching message bodies and extracts the "Order.OrderNumber" field using JSONPath</description></item>
        /// <item><description>Messages are NOT modified, moved, or deleted</description></item>
        /// <item><description>After processing, outputs all collected order numbers</description></item>
        /// </list>
        /// <para>Use Case: Extract order numbers from specific message types for analysis or reporting.</para>
        /// <para>Safe Operation: This is a read-only operation that does not affect messages.</para>
        /// <para>Note: This is a domain-specific example. Use <see cref="PrebuildExecutionSteps.CollectAndOutput.DataPointFromBodyForMessageType"/> 
        /// to create similar plans for your own message types.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var plan = PrebuildExecutionPlan.CollectAndOutput.OrderNumberFromOrderRefreshFromShopDownloadedV2(QueueName.Error.Order);
        /// await StartProgramExecution(plan);
        /// // Output: List of all order numbers found
        /// </code>
        /// </example>
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
