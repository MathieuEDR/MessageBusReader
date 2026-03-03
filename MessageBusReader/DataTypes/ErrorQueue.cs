using System;

namespace MessageBusReader.DataTypes;

internal enum ErrorQueue
{
    Unknown,
    General,
    Order,
    Product,
    Ballot
}

internal static class ErrorQueueEnumExtension
{
    internal static string GetQueueName(this ErrorQueue errorQueue)
    {
        return errorQueue switch
        {
            ErrorQueue.General => "error",
            ErrorQueue.Order => "error_order",
            ErrorQueue.Product => "error_product",
            ErrorQueue.Ballot => "error_ballot",
            _ => throw new ArgumentOutOfRangeException(nameof(errorQueue), errorQueue, null)
        };
    }

    internal static ErrorQueue ToErrorQueue(this string errorQueue)
    {
        return errorQueue switch
        {
            "error" => ErrorQueue.General,
            "error_order" => ErrorQueue.Order,
            "error_product" => ErrorQueue.Product,
            "error_ballot" => ErrorQueue.Ballot,
            _ => throw new ArgumentOutOfRangeException(nameof(errorQueue), errorQueue, null)
        };
    }
}
