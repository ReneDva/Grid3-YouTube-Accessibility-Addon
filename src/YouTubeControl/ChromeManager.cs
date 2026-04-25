// Launches Chrome with remote debugging enabled for YouTube automation.
// Resolves binary and user-data paths from config with safe fallbacks.
// Contains the ChromeManager class for Chrome startup orchestration.
using System.Diagnostics;

namespace YouTubeControl;

/// <summary>
/// Starts Chrome with the required arguments for control mode.
/// </summary>
/// <remarks>
/// Reads optional settings from config.json, resolves fallback paths, and logs startup outcomes.
/// </remarks>
internal static class ChromeManager
{
    private const string ComponentName = "ChromeManager";

    public const int DebugPort = 15432;
    public const string BrowserUrl = "http://127.0.0.1:15432";

    private static readonly string DefaultCanaryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Google",
        "Chrome SxS",
        "Application",
        "chrome.exe");

    private static readonly string DefaultStablePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Google",
        "Chrome",
        "Application",
        "chrome.exe");

    private const string FixedUserDataDir = @"C:\YouTube_User_Data_V5";

    /// <summary>
    /// Launches Chrome with the configured debugging profile.
    /// </summary>
    /// <param name="logger">The logger used for startup diagnostics.</param>
    /// <returns><see langword="true" /> when launch succeeds; otherwise, <see langword="false" />.</returns>
    public static bool Launch(Logger logger)
    {
        var chromePath = ResolveChromePath(string.Empty, logger);
        if (string.IsNullOrWhiteSpace(chromePath))
        {
            logger.Log(ComponentName, "Chrome binary was not found in configured or fallback paths.");
            return false;
        }

        var userDataDir = ResolveUserDataDirectory(logger);
        if (string.IsNullOrWhiteSpace(userDataDir))
        {
            logger.Log(ComponentName, "Unable to prepare user data directory.");
            return false;
        }

        var arguments =
            $"--remote-debugging-port={DebugPort} " +
            $"--user-data-dir=\"{userDataDir}\" " +
            "--start-maximized " +
            "--no-first-run " +
            "--no-default-browser-check " +
            "--autoplay-policy=no-user-gesture-required " +
            "--disable-session-crashed-bubble " +
            "--disable-infobars " +
            "--restore-last-session";

        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = chromePath,
                Arguments = arguments,
                UseShellExecute = true,
            });

            if (process is null)
            {
                logger.Log(ComponentName, "Process.Start returned null while launching Chrome.");
                return false;
            }

            logger.Log(ComponentName, $"Chrome launched on debugging port {DebugPort}.");
            return true;
        }
        catch (Exception ex)
        {
            logger.LogException(ComponentName, "Failed to launch Chrome", ex);
            return false;
        }
    }

    /// <summary>
    /// Resolves and prepares a writable user-data directory.
    /// </summary>
    /// <param name="logger">The logger used for directory preparation errors.</param>
    /// <returns>A usable directory path, or an empty string when unavailable.</returns>
    private static string ResolveUserDataDirectory(Logger logger)
    {
        try
        {
            Directory.CreateDirectory(FixedUserDataDir);
            return FixedUserDataDir;
        }
        catch (Exception ex)
        {
            logger.LogException(ComponentName, $"Failed preparing user data dir: {FixedUserDataDir}", ex);
        }

        return string.Empty;
    }

    /// <summary>
    /// Resolves the Chrome executable path from configured and fallback candidates.
    /// </summary>
    /// <param name="configuredPath">The configured executable path from configuration.</param>
    /// <param name="logger">The logger used for path resolution diagnostics.</param>
    /// <returns>A valid Chrome executable path, or an empty string when not found.</returns>
    private static string ResolveChromePath(string configuredPath, Logger logger)
    {
        var normalizedConfiguredPath = configuredPath.Trim().Trim('"');
        var candidates = new[]
        {
            normalizedConfiguredPath,
            DefaultCanaryPath,
            DefaultStablePath,
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (File.Exists(candidate))
            {
                logger.Log(ComponentName, $"Using Chrome binary: {candidate}");
                return candidate;
            }
        }

        logger.Log(ComponentName, $"Checked Chrome paths: {string.Join("; ", candidates.Where(p => !string.IsNullOrWhiteSpace(p)))}");
        return string.Empty;
    }
}