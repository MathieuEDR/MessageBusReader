using MessageBusReader.DataTypes;

namespace MessageBusReader.Configuration;

internal class ExecutionInputConfiguration
{
    internal required ErrorQueue TargetErrorQueue { get; init; }
    internal required string[] MessageTypesToDeleteWithoutAction { get; set; }
    internal required string[] MessageTypesToReplay { get; set; }
    internal required string[] MessagesToCollectDataFrom { get; set; }
    internal required bool DeadLetterEverythingElse { get; set; }
    public bool CollectCountByMessageType { get; set; }
}