using System.IO.Pipes;
using System.Text;

namespace YouTubeControl;

internal static class MessengerMode
{
    private const int ConnectionTimeoutMs = 2000;

    public static void SendCommand(string[] args, Logger logger)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            return;
        }

        var command = args[0].Trim();

        try
        {
            using var client = new NamedPipeClientStream(
                ".",
                Program.PipeName,
                PipeDirection.Out,
                PipeOptions.Asynchronous);

            client.Connect(ConnectionTimeoutMs);

            using var writer = new StreamWriter(client, new UTF8Encoding(false), leaveOpen: false)
            {
                AutoFlush = true,
            };

            writer.WriteLine(command);
            logger.Log($"Messenger relayed command: {command}");
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