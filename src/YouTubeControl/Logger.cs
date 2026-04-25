using System.Text;

namespace YouTubeControl;

internal sealed class Logger
{
    private readonly string _logPath;
    private readonly Lock _sync = new();

    public Logger(string logPath)
    {
        _logPath = logPath;
    }

    public void Log(string message)
    {
        try
        {
            var line = $"{DateTimeOffset.Now:O} | {message}{Environment.NewLine}";
            lock (_sync)
            {
                File.AppendAllText(_logPath, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must stay silent and never interrupt process flow.
        }
    }

    public void LogException(string context, Exception exception)
    {
        Log($"{context}: {exception.GetType().Name}: {exception.Message}");
    }
}