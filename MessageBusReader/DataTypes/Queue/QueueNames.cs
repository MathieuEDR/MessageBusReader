namespace MessageBusReader.DataTypes.Queue;

internal static class QueueNames
{
    internal static class Error
    {
        
        internal static QueueName General {get;} = new("error");
        internal static QueueName Order {get;} = new("error_order");
        internal static QueueName Product {get;} = new("error_product");
        internal static QueueName Ballot {get;} = new("error_ballot");
    }
}
