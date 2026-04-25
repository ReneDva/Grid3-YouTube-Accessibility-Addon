// Hosts leader-side pipe listening for incoming messenger commands.
// Runs an async loop with cancellation and timeout-aware accepts.
// Contains the LeaderMode class for leader process command intake.
using System.IO.Pipes;
using System.Text;

namespace YouTubeControl;

/// <summary>
/// Runs the leader-side named pipe loop for command intake.
/// </summary>
/// <remarks>
/// Accepts one connection per loop iteration, reads a single command line, and logs results
/// until <paramref name="cancellationToken" /> is canceled.
/// </remarks>
internal static class LeaderMode
{
    private const string ComponentName = "LeaderMode";

    /// <summary>
    /// Starts the leader pipe loop on a background task.
    /// </summary>
    /// <param name="logger">The logger used for command and error events.</param>
    /// <param name="cancellationToken">The cancellation token for graceful shutdown.</param>
    /// <returns>A task that represents the lifetime of the leader loop.</returns>
    public static Task RunAsync(Logger logger, CancellationToken cancellationToken)
    {
        return Task.Run(() => RunPipeServerLoopAsync(logger, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Executes the pipe server accept-read loop until cancellation is requested.
    /// </summary>
    /// <param name="logger">The logger used for command and error events.</param>
    /// <param name="cancellationToken">The cancellation token for graceful shutdown.</param>
    private static async Task RunPipeServerLoopAsync(Logger logger, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Open one server instance per accepted command.
                using var server = new NamedPipeServerStream(
                    Program.PipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                // Stop waiting after a bounded accept window.
                using var acceptTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                acceptTimeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                await server.WaitForConnectionAsync(acceptTimeoutCts.Token).ConfigureAwait(false);

                // Read one command line from the connected client.
                using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var command = await reader.ReadLineAsync().ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(command))
                {
                    logger.Log(ComponentName, $"Leader received command: {command}");
                }
                else
                {
                    logger.Log(ComponentName, "Leader received an empty command.");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown or timed-out accept.
            }
            catch (Exception ex)
            {
                logger.LogException(ComponentName, "Leader pipe loop error", ex);
            }
        }

        logger.Log(ComponentName, "Leader pipe loop stopped.");
    }
}