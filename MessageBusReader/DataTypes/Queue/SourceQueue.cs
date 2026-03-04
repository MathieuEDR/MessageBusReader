using Azure.Messaging.ServiceBus;

namespace MessageBusReader.DataTypes.Queue;

internal record SourceQueue(QueueName Name, SubQueue SubQueue)
{
}