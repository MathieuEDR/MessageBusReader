using System;
using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.DataTypes;
using MessageBusReader.ExecutionSchema.Schemas;
using MessageBusReader.Services;

namespace MessageBusReader;

using System.Threading.Tasks;

internal static class Program
{
    static async Task Main()
    {
        Console.WriteLine("Starting program");
        Console.WriteLine("Building Inputs");

        // Build inputs
        var sourceQueue = new SourceQueue(ErrorQueueName.General.GetQueueName(), SubQueue.None);
        var executionSteps = new ExecutionInputConfiguration
        {
            SourceQueue = sourceQueue,
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Analysis.AnalyzeMessagesByType()
            ]
        };
        Console.WriteLine("Inputs built");

        
        // Start Execution
        await ExecutionInitiator.Start(executionSteps);
    }
}