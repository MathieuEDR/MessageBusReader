namespace MessageBusReader.DataTypes;

internal record QueueName(string Name)
{
    public override string? ToString() => base.ToString();
}