// Writes timestamped log entries to a local file.
// Adds component-tagged and exception-focused helper overloads.
// Contains the Logger class with silent fallback behavior.
using System.Text;

namespace YouTubeControl;

/// <summary>
/// Writes timestamped diagnostic events to a log file.
/// </summary>
/// <remarks>
/// Uses a lock for thread-safe writes and falls back to a temp log if primary write fails.
/// </remarks>
internal sealed class Logger
{
    private readonly string _logPath;
    private readonly Lock _sync = new();
    private const string DefaultComponent = "General";

    /// <summary>
    /// Initializes a new instance of the Logger class.
    /// </summary>
    /// <param name="logPath">The absolute or relative path of the primary log file.</param>
    public Logger(string logPath)
    {
        // Persist configured log path.
        _logPath = logPath;

        // Ensure log directory is present.
        var directory = Path.GetDirectoryName(_logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>
    /// Writes a component-tagged message to the log.
    /// </summary>
    /// <param name="component">The component name prefix for the entry.</param>
    /// <param name="message">The log message text.</param>
    public void Log(string component, string message)
    {
        var normalizedComponent = string.IsNullOrWhiteSpace(component) ? DefaultComponent : component.Trim();
        Log($"[{normalizedComponent}] {message}");
    }

    /// <summary>
    /// Writes a raw message with timestamp to the primary log file.
    /// </summary>
    /// <param name="message">The message text to append.</param>
    public void Log(string message)
    {
        try
        {
            // Prefix with timestamp.
            var line = $"{DateTimeOffset.Now:O} | {message}{Environment.NewLine}";

            // Serialize concurrent writers.
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

    /// <summary>
    /// Writes an exception entry scoped to a specific component.
    /// </summary>
    /// <param name="component">The component name prefix for the entry.</param>
    /// <param name="context">The operation context for the exception.</param>
    /// <param name="exception">The exception to format and log.</param>
    public void LogException(string component, string context, Exception exception)
    {
        var stackTrace = string.IsNullOrWhiteSpace(exception.StackTrace) ? "n/a" : exception.StackTrace;
        Log(component, $"{context}: {exception.GetType().Name}: {exception.Message}{Environment.NewLine}{stackTrace}");
    }

    /// <summary>
    /// Writes an exception entry using the default component tag.
    /// </summary>
    /// <param name="context">The operation context for the exception.</param>
    /// <param name="exception">The exception to format and log.</param>
    public void LogException(string context, Exception exception)
    {
        LogException(DefaultComponent, context, exception);
    }

    /// <summary>
    /// Attempts a fallback log write when the primary log write path fails.
    /// </summary>
    /// <param name="loggingException">The exception raised during primary log write.</param>
    /// <param name="originalMessage">The original message that failed to persist.</param>
    private void TryWriteFallback(Exception loggingException, string originalMessage)
    {
        try
        {
            // Write fallback entry in temp directory.
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