// src\YouTubeControl\Logger.cs
// This file contains the implementation of the Logger class, which provides simple logging functionality for the application. The Logger writes log messages to a specified file, including timestamps for each entry. It also provides a method for logging exceptions with contextual information. The Logger is designed to be thread-safe and to fail silently if any issues occur during logging, ensuring that it does not interfere with the main application flow.
using System.Text;

namespace YouTubeControl;

internal sealed class Logger
{
    private readonly string _logPath;
    private readonly Lock _sync = new();

    public Logger(string logPath)
    {
        _logPath = logPath;

        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
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
        catch (Exception loggingException)
        {
            TryWriteFallback(loggingException, message);
        }
    }

    public void LogException(string context, Exception exception)
    {
        var stackTrace = string.IsNullOrWhiteSpace(exception.StackTrace) ? "n/a" : exception.StackTrace;
        Log($"{context}: {exception.GetType().Name}: {exception.Message}{Environment.NewLine}{stackTrace}");
    }

    private void TryWriteFallback(Exception loggingException, string originalMessage)
    {
        try
        {
            var fallbackPath = Path.Combine(Path.GetTempPath(), "YouTubeControl.fallback.log");
            var fallbackLine =
                $"{DateTimeOffset.Now:O} | Primary log write failed ({loggingException.GetType().Name}: {loggingException.Message}) | Original message: {originalMessage}{Environment.NewLine}";

            lock (_sync)
            {
                File.AppendAllText(fallbackPath, fallbackLine, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must stay silent and never interrupt process flow.
        }
    }
}