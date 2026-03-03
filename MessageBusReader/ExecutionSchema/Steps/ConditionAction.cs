using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace MessageBusReader.ExecutionSchema.Steps;

internal class ConditionAction
{
    public required Func<ProcessMessageEventArgs, bool> Condition { get; init; }
    public required Func<ProcessMessageEventArgs, Task> Action { get; init; }
    public Func<Task>? ExecutionFinishedCallback { get; init; }
}