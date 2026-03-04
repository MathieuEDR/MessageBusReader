using System.Collections.Generic;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.ExecutionSchema.Steps;

namespace MessageBusReader.Configuration;

internal class ExecutionInputConfiguration
{
    internal required SourceQueue SourceQueue { get; init; }
    public required List<ConditionAction> ExecutionSteps { get; init; } = new();
}