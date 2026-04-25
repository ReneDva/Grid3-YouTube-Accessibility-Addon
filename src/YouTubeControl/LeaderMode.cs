// src\YouTubeControl\LeaderMode.cs
// This file contains the implementation of the LeaderMode, which is responsible for running a named pipe server loop that listens for incoming commands from messenger instances. The LeaderMode runs in a background task and handles incoming connections asynchronously. It logs received commands and any errors that occur during the pipe server loop. The loop continues until the provided cancellation token is triggered, at which point it performs cleanup and logs that it has stopped.
using System.IO.Pipes;
using System.Text;

namespace YouTubeControl;

internal static class LeaderMode
{
    private const string ComponentName = "LeaderMode";

    public static Task RunAsync(Logger logger, CancellationToken cancellationToken)
    {
        /* The leader mode runs a loop that creates a NamedPipeServerStream and waits for incoming connections. When a client connects, it reads a command from the pipe and logs it. The loop continues until the cancellation token is triggered, at which point it exits gracefully. Any exceptions that occur during the loop are caught and logged, ensuring that the leader mode remains robust and does not crash unexpectedly.
         */
        return Task.Run(() => RunPipeServerLoopAsync(logger, cancellationToken), cancellationToken);
    }

    private static async Task RunPipeServerLoopAsync(Logger logger, CancellationToken cancellationToken)
    {
        /* The pipe server loop continuously creates a NamedPipeServerStream and waits for clients to connect. It uses an asynchronous wait with a cancellation token to allow for graceful shutdown. When a client connects, it reads a command from the pipe and logs it. If the command is empty or whitespace, it logs that an empty command was received. The loop handles OperationCanceledException to allow for expected shutdown scenarios and logs any other exceptions that may occur during the pipe operations.
         */
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Create a new named pipe server stream for each connection. This allows multiple clients to connect sequentially without needing to manage multiple server instances.
                using var server = new NamedPipeServerStream(
                    Program.PipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
                
                using var acceptTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                acceptTimeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                await server.WaitForConnectionAsync(acceptTimeoutCts.Token).ConfigureAwait(false);

                // Once a client is connected, read the command from the pipe. The command is expected to be a single line of text. If the command is null, empty, or whitespace, log that an empty command was received.
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
                // Expected on shutdown or when the accept timeout expires.
            }
            catch (Exception ex)
            {
                logger.LogException(ComponentName, "Leader pipe loop error", ex);
            }
        }

        logger.Log(ComponentName, "Leader pipe loop stopped.");
    }
}