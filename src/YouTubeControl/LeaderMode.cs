using System.IO.Pipes;
using System.Text;

namespace YouTubeControl;

internal static class LeaderMode
{
    public static Task RunAsync(Logger logger, CancellationToken cancellationToken)
    {
        return Task.Run(() => RunPipeServerLoopAsync(logger, cancellationToken), cancellationToken);
    }

    private static async Task RunPipeServerLoopAsync(Logger logger, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    Program.PipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                using var acceptTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                acceptTimeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                await server.WaitForConnectionAsync(acceptTimeoutCts.Token).ConfigureAwait(false);

                using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var command = await reader.ReadLineAsync().ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(command))
                {
                    logger.Log($"Leader received command: {command}");
                }
                else
                {
                    logger.Log("Leader received an empty command.");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown or when the accept timeout expires.
            }
            catch (Exception ex)
            {
                logger.LogException("Leader pipe loop error", ex);
            }
        }

        logger.Log("Leader pipe loop stopped.");
    }
}