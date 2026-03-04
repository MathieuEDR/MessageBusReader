using Azure.Messaging.ServiceBus;

namespace MessageBusReader.DataTypes;

internal record SourceQueue(QueueName Name, SubQueue SubQueue)
{
}