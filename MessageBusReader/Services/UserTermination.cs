using System;
using System.Threading;
using System.Threading.Tasks;
using MessageBusReader.Services.Logging;

namespace MessageBusReader.Services;

internal class UserTermination
{
    public UserTermination()
    {
        RegisterConsoleCancelEvent();
    }

    private readonly TaskCompletionSource<int> _userTerminationTaskSource = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Logger _logger = new(nameof(UserTermination));

    internal async Task WaitUntilUserTerminatesProgram()
    {
        _logger.Log("Waiting for messages. Press CTRL+C or force-quit the app to exit");
        await _userTerminationTaskSource.Task;
        _logger.Log("Terminating program");
    }

    private void RegisterConsoleCancelEvent()
    {
        // Register for console cancel event to gracefully shutdown
        Console.CancelKeyPress += async (sender, cancelEvent) => await HandleCancellation(cancelEvent);
    }

    private async Task HandleCancellation(ConsoleCancelEventArgs cancelEvent)
    {
        cancelEvent.Cancel = true;
        await _cancellationTokenSource.CancelAsync();
        _userTerminationTaskSource?.TrySetResult(0);
    }
}
