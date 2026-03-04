using Azure.Messaging.ServiceBus;

namespace MessageBusReader.DataTypes.Queue;

internal record Queue(QueueName Name, SubQueue SubQueue = SubQueue.None)
{
}