using System;
using Azure.Messaging.ServiceBus;

namespace MessageBusReader.DataTypes;

internal enum ErrorQueueName
{
    Unknown,
    General,
    Order,
    Product,
    Ballot
}

internal static class ErrorQueueEnumExtension
{
    internal static QueueName GetQueueName(this ErrorQueueName errorQueueName)
    {
        return errorQueueName switch
        {
            ErrorQueueName.General => new QueueName("error"),
            ErrorQueueName.Order => new QueueName("error_order"),
            ErrorQueueName.Product => new QueueName("error_product"),
            ErrorQueueName.Ballot => new QueueName("error_ballot"),
            _ => throw new ArgumentOutOfRangeException(nameof(errorQueueName), errorQueueName, null)
        };
    }

    internal static ErrorQueueName ToErrorQueue(this string errorQueue)
    {
        return errorQueue switch
        {
            "error" => ErrorQueueName.General,
            "error_order" => ErrorQueueName.Order,
            "error_product" => ErrorQueueName.Product,
            "error_ballot" => ErrorQueueName.Ballot,
            _ => throw new ArgumentOutOfRangeException(nameof(errorQueue), errorQueue, null)
        };
    }
}

internal record QueueName(string Name)
{
    public override string? ToString() => base.ToString();
}

internal record TargetQueue(QueueName Name)
{
}

internal record SourceQueue(QueueName Name, SubQueue SubQueue)
{
}
