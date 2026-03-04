using MessageBusReader.Configuration;
using MessageBusReader.Services;
using System.Linq;
using MessageBusReader.DataTypes.Queue;
using MessageBusReader.ExecutionSchema.Prebuilt;
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

        // Build inputs
        var sourceQueueName = QueueName.Error.General;
        var executionConfiguration = PrebuildExecutionPlan.CollectAndOutput.OrderNumberFromOrderRefreshFromShopDownloadedV2(sourceQueueName);
        
        // Start Execution
        await StartProgramExecution(executionConfiguration);
    }


    private static async Task StartProgramExecution(ExecutionPlan inputs)
    {
        var processor = new QueueProcessor(inputs);

        await processor.Start();

        await UserTermination.WaitUntilUserTerminatesProgram();

        await processor.Stop();

        await Dispose(processor);

        await ExecuteFinishedCallbacks(inputs);
    }

    private static async Task ExecuteFinishedCallbacks(ExecutionPlan inputs)
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
