// Builds command text from process arguments.
// Connects to the leader pipe and forwards one command line.
// Contains the MessengerMode class for messenger-side relay logic.
using System.IO.Pipes;
using System.Text;

namespace YouTubeControl;

/// <summary>
/// Relays commands from a messenger instance to the active leader instance.
/// </summary>
/// <remarks>
/// Converts input arguments into a single command line and writes it to the shared named pipe.
/// </remarks>
internal static class MessengerMode
{
    private const int ConnectionTimeoutMs = 2000;
    private const string ComponentName = "MessengerMode";

    /// <summary>
    /// Builds a normalized command line from input arguments.
    /// </summary>
    /// <param name="args">The process arguments to combine into one command string.</param>
    /// <returns>A trimmed command line, or <see langword="null" /> when input is empty.</returns>
    internal static string? BuildCommand(string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var command = string.Join(" ", args).Trim();
        return string.IsNullOrWhiteSpace(command) ? null : command;
    }

    /// <summary>
    /// Sends the command represented by <paramref name="args" /> to the leader pipe.
    /// </summary>
    /// <param name="args">The process arguments to forward.</param>
    /// <param name="logger">The logger used for timeout and error reporting.</param>
    public static void SendCommand(string[] args, Logger logger)
    {
        var command = BuildCommand(args);
        if (command is null)
        {
            return;
        }

        try
        {
            using var client = new NamedPipeClientStream(
                ".",
                Program.PipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous);

            client.Connect(ConnectionTimeoutMs);

            using var writer = new StreamWriter(client, Encoding.UTF8, leaveOpen: false)
            {
                AutoFlush = true,
            };

            // Send one line so leader can ReadLineAsync.
            writer.WriteLine(command);
        }
        catch (TimeoutException)
        {
            logger.Log(ComponentName, "Messenger timeout while connecting to leader pipe.");
        }
        catch (Exception ex)
        {
            logger.LogException(ComponentName, "Messenger send failed", ex);
        }
    }
}