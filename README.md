# MessageBusReader

A powerful and flexible tool for managing Azure Service Bus messages with prebuilt execution plans and custom workflows.

WARNING - BEWARE - DO NOT JUST RUN THIS APP WITHOUT LOOKING AT IT
This app has the potential to be HIGHLY destructive. If commands are removed the queue with without being re-processed, the data they contain IS LOST FOREVER.

This version of the app may never be used by anyone other than me, and this code may never be read by anyone but me. Just to be on the safe side, if you are uncertain of what this does, or how to use it, rather than just executing it and "see what happens" or asking and AI/LLM, ask the human Mathieu Viales, at Edrington. I don't bite. 

## Rule of thumb
If the `Program.cs` contains this text `PrebuildExecutionPlan.Execute`, executing the program will take actions against the queue *that hae side-effect*. This can range from re-sending a message to be processed as-is or wiht modifications to its payload, all the way to outright deleting every message in the queue. 

If the `Program.cs` contains this text `PrebuildExecutionPlan.CollectAndOutput`, the execution of the program will be pure (at least with regards to ServiceBus) and is non-destructive.

## Overview

MessageBusReader is a C# application designed to process, filter, and manage messages in Azure Service Bus queues. It provides both high-level prebuilt execution plans for common scenarios and low-level APIs for building custom message processing workflows.

## Why would you do this, Mathieu ?
The existing app is minefield. Managing execution by commenting and uncommenting specific lines, copy/pasting code from one array to an other, that's a recipe for me to make a disaster. 
I don't trust myself to use the existing app safely enough, especially if I were to use it in an emergency. Too many moving parts, to many things to configure in too many places. 
I have designed and built this app with the specific goal in mind to reduce the amount of tinkering required to achieve a specific goal, and to abstract away as much as possible of the inner working of the app to enable the developper that is using to focus on exactly what side-effect they are hoping to achieve, not on how to achieve it wiggling around ServiceBus specifics.

## Table of Contents

- [Quick Start](#quick-start)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
  - [Using Prebuilt Execution Plans](#using-prebuilt-execution-plans)
  - [Using Prebuilt Execution Steps](#using-prebuilt-execution-steps)
  - [Building Custom Workflows](#building-custom-workflows)
- [API Reference](#api-reference)
- [Advanced Examples](#advanced-examples)

## Quick Start

### Basic Usage

The simplest way to use MessageBusReader is with a prebuilt execution plan:

```csharp
using MessageBusReader.Configuration;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.ExecutionSchema.Prebuilt;

var sourceQueueName = QueueName.Error.General;
var executionConfiguration = PrebuildExecutionPlan.Execute.ReplayMessagedOfType(
    sourceQueueName, 
    "Edrington.Contracts.Orders.Events.OrderRefreshFromShopDownloadedV2, Edrington.Contracts.Orders"
);

await StartProgramExecution(executionConfiguration);
```

## Configuration

### Queue Names

Queue names are defined using the `QueueName` record:

```csharp
// Using predefined queue names
var errorQueue = QueueName.Error.General;
var orderQueue = QueueName.Error.Order;
var productQueue = QueueName.Error.Product;
var ballotQueue = QueueName.Error.Ballot;

// Creating custom queue names
var customQueue = new QueueName("my-custom-queue");
```

### Queue Types

Queues can include sub-queues (e.g., dead-letter queues):

```csharp
// Main queue
var mainQueue = new Queue(QueueName.Error.General);

// Dead-letter queue
var deadLetterQueue = new Queue(QueueName.Error.General, SubQueue.DeadLetter);
```

The sub-queue is only taken into account when used as the "source". It does not matter when using this queue as a target.

## Usage Examples

### Using Prebuilt Execution Plans

Prebuilt execution plans provide ready-to-use configurations for common scenarios.

#### 1. Return All Messages from Dead Letter Queue

```csharp
var plan = PrebuildExecutionPlan.Execute.ReturnAllFromDeadLetter(QueueName.Error.Order);
await StartProgramExecution(plan);
```

This plan:
- Reads all messages from the dead-letter queue
- Sends them back to the main queue

#### 2. Replay Messages of Specific Types

```csharp
var plan = PrebuildExecutionPlan.Execute.ReplayMessagedOfType(
    QueueName.Error.General,
    "MyApp.Events.OrderCreated, MyApp.Contracts",
    "MyApp.Events.OrderUpdated, MyApp.Contracts"
);
await StartProgramExecution(plan);
```

This plan:
- Filters messages by type
- Returns matching messages to their source queue

#### 3. Count Messages by Type

```csharp
var plan = PrebuildExecutionPlan.CollectAndOutput.CountByMessageType(QueueName.Error.General);
await StartProgramExecution(plan);
```

This plan:
- Counts all messages grouped by message type
- Outputs the results when execution finishes

#### 4. Extract Data Points from Messages

```csharp
var plan = PrebuildExecutionPlan.CollectAndOutput.OrderNumberFromOrderRefreshFromShopDownloadedV2(
    QueueName.Error.Order
);
await StartProgramExecution(plan);
```

This plan:
- Filters for specific message types
- Extracts order numbers from the message body
- Outputs all collected data when execution finishes

### Using Prebuilt Execution Steps

For more control, you can build your own execution plan using prebuilt steps.

#### Example 1: Return All Messages to Source Queue

```csharp
var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.General, SubQueue.DeadLetter),
    ExecutionSteps =
    [
        PrebuildExecutionSteps.Execute.ReturnAllToSourceQueue()
    ]
};
await StartProgramExecution(executionPlan);
```

#### Example 2: Delete Specific Message Types

```csharp
var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.General),
    ExecutionSteps =
    [
        PrebuildExecutionSteps.Execute.DeleteMessagesOfType(
            "MyApp.Events.ObsoleteEvent, MyApp.Contracts"
        )
    ]
};
await StartProgramExecution(executionPlan);
```

#### Example 3: Multi-Step Processing

```csharp
var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.General),
    ExecutionSteps =
    [
        // First, count all messages by type
        PrebuildExecutionSteps.CollectAndOutput.CountByMessageType(),
        
        // Then, return specific types to their source queue
        PrebuildExecutionSteps.Execute.ReturnMessagesOfType(
            "MyApp.Events.OrderCreated, MyApp.Contracts"
        ),
        
        // Finally, delete other specific types
        PrebuildExecutionSteps.Execute.DeleteMessagesOfType(
            "MyApp.Events.TestEvent, MyApp.Contracts"
        )
    ]
};
await StartProgramExecution(executionPlan);
```

#### Example 4: Send All Messages to Another Queue

```csharp
var destinationQueue = new Queue(new QueueName("archive-queue"));

var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.General),
    ExecutionSteps =
    [
        PrebuildExecutionSteps.Execute.SendAllToQueue(destinationQueue)
    ]
};
await StartProgramExecution(executionPlan);
```

### Building Custom Workflows

For maximum flexibility, create custom workflows using `ConditionAction`, `MessageFilter`, and `MessageAction`.

#### The Building Blocks

1. **ConditionAction**: Defines a condition and an action to execute when the condition is met
2. **MessageFilter**: Provides methods to filter messages
3. **MessageAction**: Provides methods to perform actions on messages

#### Example 1: Basic Custom Filter and Action

```csharp
var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.General),
    ExecutionSteps =
    [
        new ConditionAction
        {
            // Filter: Include only messages of specific types
            Condition = message => MessageFilter.Include.OfType(
                message,
                "MyApp.Events.CustomerCreated, MyApp.Contracts"
            ),
            // Action: Return to source queue
            Action = message => message.ReturnToSourceQueue()
        }
    ]
};
await StartProgramExecution(executionPlan);
```

#### Example 2: Custom Filter with Multiple Conditions

```csharp
var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.Order),
    ExecutionSteps =
    [
        new ConditionAction
        {
            // Custom condition: Check message type and property
            Condition = message =>
            {
                var isCorrectType = MessageFilter.Include.OfType(
                    message,
                    "MyApp.Events.OrderCreated, MyApp.Contracts"
                );
                
                // Additional custom logic
                var hasRetryProperty = message.Message.ApplicationProperties
                    .ContainsKey("RetryCount");
                
                return isCorrectType && !hasRetryProperty;
            },
            Action = message => message.ReturnToSourceQueue()
        }
    ]
};
await StartProgramExecution(executionPlan);
```

#### Example 3: Custom Action with Logging

```csharp
var logger = new Logger(nameof(Program));

var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.General),
    ExecutionSteps =
    [
        new ConditionAction
        {
            Condition = MessageFilter.Include.ForAll,
            Action = async message =>
            {
                var messageType = ProcessMessageEventArgsExtensions.GetType(message);
                logger.Log($"Processing message: {message.Message.MessageId} of type {messageType?.Value}");
                
                // Perform custom action
                await message.ReturnToSourceQueue();
            }
        }
    ]
};
await StartProgramExecution(executionPlan);
```

#### Example 4: Extract and Process Message Data

```csharp
using Newtonsoft.Json.Linq;

var collectedData = new List<string>();

var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.Order),
    ExecutionSteps =
    [
        new ConditionAction
        {
            Condition = message => MessageFilter.Include.OfType(
                message,
                "MyApp.Events.OrderCreated, MyApp.Contracts"
            ),
            Action = async message =>
            {
                // Deserialize message body
                var body = message.Deserialize();
                var json = JObject.Parse(body);
                
                // Extract data
                var orderId = json.SelectToken("$.OrderId")?.Value<string>();
                var customerEmail = json.SelectToken("$.Customer.Email")?.Value<string>();
                
                if (orderId != null)
                {
                    collectedData.Add($"Order: {orderId}, Email: {customerEmail}");
                }
                
                // Don't delete the message
                await Task.CompletedTask;
            },
            ExecutionFinishedCallback = () =>
            {
                Console.WriteLine("Collected Data:");
                foreach (var data in collectedData)
                {
                    Console.WriteLine(data);
                }
                return Task.CompletedTask;
            }
        }
    ]
};
await StartProgramExecution(executionPlan);
```

#### Example 5: Route Messages Based on Content

```csharp
var highPriorityQueue = new Queue(new QueueName("orders-high-priority"));
var normalQueue = new Queue(new QueueName("orders-normal"));

var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.Order),
    ExecutionSteps =
    [
        new ConditionAction
        {
            Condition = message => MessageFilter.Include.OfType(
                message,
                "MyApp.Events.OrderCreated, MyApp.Contracts"
            ),
            Action = async message =>
            {
                var body = message.Deserialize();
                var json = JObject.Parse(body);
                var totalAmount = json.SelectToken("$.TotalAmount")?.Value<decimal>() ?? 0;
                
                // Route based on order amount
                var targetQueue = totalAmount > 1000 ? highPriorityQueue : normalQueue;
                
                await MessageAction.SendToQueue(message, targetQueue);
            }
        }
    ]
};
await StartProgramExecution(executionPlan);
```

#### Example 6: Conditional Deletion with Logging

```csharp
var deletedCount = 0;

var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.General),
    ExecutionSteps =
    [
        new ConditionAction
        {
            // Delete old test messages
            Condition = message =>
            {
                var isTestMessage = message.Message.ApplicationProperties
                    .TryGetValue("Environment", out var env) && env?.ToString() == "Test";
                
                var isOld = message.Message.EnqueuedTime < DateTime.UtcNow.AddDays(-7);
                
                return isTestMessage && isOld;
            },
            Action = async message =>
            {
                await message.CompleteMessageAsync(message.Message, CancellationToken.None);
                deletedCount++;
            },
            ExecutionFinishedCallback = () =>
            {
                Console.WriteLine($"Deleted {deletedCount} old test messages");
                return Task.CompletedTask;
            }
        }
    ]
};
await StartProgramExecution(executionPlan);
```

## API Reference

### PrebuildExecutionPlan.Execute

| Method | Description |
|--------|-------------|
| `ReturnAllFromDeadLetter(QueueName)` | Returns all messages from a dead-letter queue to the main queue |
| `ReplayMessagedOfType(QueueName, params string[])` | Replays messages of specific types to their source queue |

### PrebuildExecutionPlan.CollectAndOutput

| Method | Description |
|--------|-------------|
| `CountByMessageType(QueueName)` | Counts messages by type and outputs results |
| `OrderNumberFromOrderRefreshFromShopDownloadedV2(QueueName)` | Extracts order numbers from specific message types |

### PrebuildExecutionSteps.Execute

| Method | Description |
|--------|-------------|
| `ReturnAllToSourceQueue()` | Returns all messages to their original source queue |
| `ReturnMessagesOfType(params string[])` | Returns specific message types to their source queue |
| `DeleteMessagesOfType(params string[])` | Deletes messages of specific types |
| `DeleteAll()` | Deletes all messages |
| `SendAllToQueue(Queue)` | Sends all messages to a specified queue |

### PrebuildExecutionSteps.CollectAndOutput

| Method | Description |
|--------|-------------|
| `CountByMessageType()` | Collects count statistics by message type |
| `DataPointFromBodyForMessageType(string, params string[])` | Extracts data from message bodies using JSONPath |

### MessageFilter.Include

| Method | Description |
|--------|-------------|
| `ForAll(ProcessMessageEventArgs)` | Includes all messages (always returns true) |
| `OfType(ProcessMessageEventArgs, params string[])` | Includes messages of specific types |

### MessageAction

| Method | Description |
|--------|-------------|
| `SendToQueue(ProcessMessageEventArgs, Queue)` | Sends a message to a specified queue |

### Extension Methods (ProcessMessageEventArgsExtensions)

| Method | Description |
|--------|-------------|
| `GetType()` | Gets the message type from application properties |
| `IsOfType(params string[])` | Checks if message matches any of the specified types |
| `GetSourceQueue()` | Gets the original source queue from message properties |
| `ReturnToSourceQueue(int)` | Returns message to its source queue (optionally with delay) |
| `Delete()` | Completes and deletes the message |
| `Deserialize()` | Deserializes message body to string |

## Advanced Examples

### Combining Multiple Conditions

```csharp
var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.General),
    ExecutionSteps =
    [
        // Step 1: Process high-value orders
        new ConditionAction
        {
            Condition = message =>
            {
                if (!MessageFilter.Include.OfType(message, "MyApp.Events.OrderCreated, MyApp.Contracts"))
                    return false;
                
                var body = message.Deserialize();
                var json = JObject.Parse(body);
                return json.SelectToken("$.TotalAmount")?.Value<decimal>() > 1000;
            },
            Action = message => message.ReturnToSourceQueue()
        },
        
        // Step 2: Archive low-value orders
        new ConditionAction
        {
            Condition = message =>
            {
                if (!MessageFilter.Include.OfType(message, "MyApp.Events.OrderCreated, MyApp.Contracts"))
                    return false;
                
                var body = message.Deserialize();
                var json = JObject.Parse(body);
                return json.SelectToken("$.TotalAmount")?.Value<decimal>() <= 1000;
            },
            Action = message => MessageAction.SendToQueue(message, new Queue(new QueueName("archive")))
        }
    ]
};
```

### Processing with Callbacks

```csharp
var processedCount = 0;
var errorCount = 0;

var executionPlan = new ExecutionPlan
{
    SourceQueue = new Queue(QueueName.Error.General),
    ExecutionSteps =
    [
        new ConditionAction
        {
            Condition = MessageFilter.Include.ForAll,
            Action = async message =>
            {
                try
                {
                    await message.ReturnToSourceQueue();
                    processedCount++;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
            },
            ExecutionFinishedCallback = () =>
            {
                Console.WriteLine($"Processing complete!");
                Console.WriteLine($"Successfully processed: {processedCount}");
                Console.WriteLine($"Errors: {errorCount}");
                return Task.CompletedTask;
            }
        }
    ]
};
```

## Best Practices

- **Use Prebuilt Plans & Steps**:
  - Start with prebuilt execution plans for common scenarios
  - Use prebuilt steps, and potentially create a new pre-built plan for common scenarios
  - Avoid building an execution plan manually.
- **Use Callbacks**: Leverage `ExecutionFinishedCallback` for summary reports and cleanup
- **Log Operations**: Use the built-in logging for tracking message processing

## Exiting the program

The application runs until manually terminated. Press `Ctrl+C` to stop processing and trigger execution finished callbacks.
