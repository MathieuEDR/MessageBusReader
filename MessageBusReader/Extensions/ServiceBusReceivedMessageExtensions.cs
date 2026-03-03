using System.Text;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json.Linq;

namespace MessageBusReader.Extensions;

internal static class ServiceBusReceivedMessageExtensions
{
    private const string ErrorKey = "rbs2-error-details";

    internal static bool ContainsError(this ServiceBusReceivedMessage message, string error)
    {
        if (message.ApplicationProperties.ContainsKey(ErrorKey))
        {
            var errorMessage = message.ApplicationProperties[ErrorKey].ToString();

            return errorMessage.Contains(error);
        }

        return false;
    }

    internal static string GetErrorMessage(this ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.ContainsKey(ErrorKey))
        {
            return message.ApplicationProperties[ErrorKey].ToString();
        }

        return string.Empty;
    }

    internal static string BodyAsString(this ServiceBusReceivedMessage message)
    {
        return Encoding.UTF8.GetString(message.Body);
    }

    internal static dynamic BodyAsDynamic(this ServiceBusReceivedMessage message)
    {
        var body = message.BodyAsString();

        dynamic msg = JObject.Parse(body);

        return msg;
    }
}
