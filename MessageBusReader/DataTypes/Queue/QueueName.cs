namespace MessageBusReader.DataTypes.Queue;

internal record QueueName(string Name)
{
    public override string? ToString() => base.ToString();

    internal static class Error

    {
        internal static QueueName General { get; } = new("error");
        internal static QueueName Order { get; } = new("error_order");
        internal static QueueName Product { get; } = new("error_product");
        internal static QueueName Ballot { get; } = new("error_ballot");
    }
}
