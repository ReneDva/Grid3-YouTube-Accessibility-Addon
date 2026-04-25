// src\YouTubeControl\Logger.cs
// This file contains the implementation of the Logger class, which provides simple logging functionality for the application. The Logger writes log messages to a specified file, including timestamps for each entry. It also provides a method for logging exceptions with contextual information. The Logger is designed to be thread-safe and to fail silently if any issues occur during logging, ensuring that it does not interfere with the main application flow.
using System.Text;

namespace YouTubeControl;

// A simple logger that writes messages to a specified log file with timestamps. It includes error handling to ensure that logging failures do not disrupt the main application flow.
internal sealed class Logger
{
    private readonly string _logPath;
    private readonly Lock _sync = new();
    private const string DefaultComponent = "General";

    public Logger(string logPath)
    {
        // Ensure the log directory exists
        _logPath = logPath;

        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void Log(string component, string message)
    {
        var normalizedComponent = string.IsNullOrWhiteSpace(component) ? DefaultComponent : component.Trim();
        Log($"[{normalizedComponent}] {message}");
    }

    // Logs a message to the log file with a timestamp. If logging fails, it attempts to write the failure details to a fallback log file in the system's temporary directory.
    public void Log(string message)
    {
        try
        {
            // Prepend a timestamp to the log message
            var line = $"{DateTimeOffset.Now:O} | {message}{Environment.NewLine}";
            // Ensure that log writes are thread-safe
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

    public void LogException(string component, string context, Exception exception)
    {
        var stackTrace = string.IsNullOrWhiteSpace(exception.StackTrace) ? "n/a" : exception.StackTrace;
        Log(component, $"{context}: {exception.GetType().Name}: {exception.Message}{Environment.NewLine}{stackTrace}");
    }

    // Logs an exception with contextual information. It formats the exception details and stack trace into a readable format before logging. If logging fails, it attempts to write the failure details to a fallback log file.
    public void LogException(string context, Exception exception)
    {
        LogException(DefaultComponent, context, exception);
    }

    // Attempts to write a fallback log entry if the primary log write fails. This ensures that logging failures do not go completely unnoticed.
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