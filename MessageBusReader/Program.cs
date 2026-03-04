using Azure.Messaging.ServiceBus;
using MessageBusReader.Configuration;
using MessageBusReader.ExecutionSchema.Schemas;
using MessageBusReader.Services;
using System.Linq;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.Services.Logging;
using MessageBusReader.Services.Processors;
using MessageBusReader.Services.ServiceBus;

namespace MessageBusReader;

using System.Threading.Tasks;

internal static class Program
{
    private static readonly Logger Logger = new(nameof(Program));
    private static readonly UserTermination UserTermination = new();

    static async Task Main()
    {
        Logger.Log("Starting program");
        Logger.Log("Building Inputs");

        // Build inputs
        var executionConfiguration = new ExecutionInputConfiguration
        {
            SourceQueue = new Queue(QueueNames.Error.General),
            ExecutionSteps =
            [
                PrebuildExecutionSteps.Execute.ReplayAll()
            ]
        };
        
        
        // Start Execution
        await StartProgramExecution(executionConfiguration);
    }


    private static async Task StartProgramExecution(ExecutionInputConfiguration inputs)
    {
        var processor = new QueueProcessor(inputs);

        await processor.Start();

        await UserTermination.WaitUntilUserTerminatesProgram();

        await processor.Stop();

        await Dispose(processor);

        await ExecuteFinishedCallbacks(inputs);
    }

    private static async Task ExecuteFinishedCallbacks(ExecutionInputConfiguration inputs)
    {
        var inputsExecutionSteps = inputs.ExecutionSteps;
        var callbacksCount = inputsExecutionSteps.Count(step => step.ExecutionFinishedCallback != null);
        Logger.Log($"There are {callbacksCount} callbacks to execute");

        foreach (var executionStep in inputsExecutionSteps)
        {
            if (executionStep.ExecutionFinishedCallback != null)
            {
                Logger.Log("Executing callback");
                await executionStep.ExecutionFinishedCallback();
            }
        }
    }

    private static async Task Dispose(QueueProcessor processor)
    {
        await processor.DisposeAsync();
        await ServiceBusClientProvider.DisposeAsync();
    }
}
