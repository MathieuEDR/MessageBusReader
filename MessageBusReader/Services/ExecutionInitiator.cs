using System.Linq;
using System.Threading.Tasks;
using MessageBusReader.Configuration;

namespace MessageBusReader.Services;

internal static class ExecutionInitiator
{
    private static readonly Logger Logger = new(nameof(ExecutionInitiator));
    private static readonly UserTermination UserTermination = new();

    internal static async Task Start(ExecutionInputConfiguration inputs)
    {
        var processor = new QueueProcessor(inputs);

        await processor.Start();

        await UserTermination.WaitUntilUserTerminatesProgram();

        await processor.Stop();

        await Dispose(processor);

        await ExecuteCallbacks(inputs);
    }

    private static async Task Dispose(QueueProcessor processor)
    {
        await processor.DisposeAsync();
        await ServiceBusClientProvider.DisposeAsync();
    }


    private static async Task ExecuteCallbacks(ExecutionInputConfiguration inputs)
    {
        var callbacksCount = inputs.ExecutionSteps.Count(step => step.ExecutionFinishedCallback != null);
        Logger.Log($"There are {callbacksCount} callbacks to execute");

        foreach (var executionStep in inputs.ExecutionSteps)
        {
            if (executionStep.ExecutionFinishedCallback != null)
            {
                Logger.Log("Executing callback");
                await executionStep.ExecutionFinishedCallback();
            }
        }
    }
}
