namespace MessageBusReader.DataTypes.Queue;

internal record QueueName(string Name)
{
    public override string? ToString() => base.ToString();
}