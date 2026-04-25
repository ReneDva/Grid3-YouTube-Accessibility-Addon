// src\YouTubeControl\MessengerMode.cs
// This file contains the implementation of the MessengerMode, which is responsible for sending commands to the leader instance via a named pipe. It attempts to connect to the leader's pipe and send the command, logging any errors that occur during the process. If the connection times out or fails, it logs the appropriate messages without throwing exceptions to the caller.
using System.IO.Pipes;
using System.Text;

namespace YouTubeControl;

internal static class MessengerMode
{
    private const int ConnectionTimeoutMs = 2000;

    internal static string? BuildCommand(string[] args)
    {
        if (args.Length == 0)
        {
            return null;
        }

        var command = string.Join(" ", args).Trim();
        return string.IsNullOrWhiteSpace(command) ? null : command;
    }

    // Sends the command represented by the args array to the leader instance via a named pipe. If the connection or sending fails, it logs the error and returns without throwing exceptions.
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

            writer.WriteLine(command);
        }
        catch (TimeoutException)
        {
            logger.Log("Messenger timeout while connecting to leader pipe.");
        }
        catch (Exception ex)
        {
            logger.LogException("Messenger send failed", ex);
        }
    }
}