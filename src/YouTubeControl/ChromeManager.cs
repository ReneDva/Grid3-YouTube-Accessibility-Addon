using System.Diagnostics;
using YouTubeControl.Models;

namespace YouTubeControl;

internal static class ChromeManager
{
    private const string ComponentName = "ChromeManager";
    private const int DebugPort = 15432;
    private const string StartupUrl = "https://www.youtube.com";

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

    private static readonly string FallbackUserDataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YouTubeControl",
        "UserData");

    public static bool Launch(Logger logger)
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        var appConfig = AppConfig.LoadOrDefault(configPath, logger);

        var chromePath = ResolveChromePath(appConfig.ChromePath ?? string.Empty, logger);
        if (string.IsNullOrWhiteSpace(chromePath))
        {
            logger.Log(ComponentName, "Chrome binary was not found in configured or fallback paths.");
            return false;
        }

        var userDataDir = ResolveUserDataDirectory(appConfig.UserDataDir ?? string.Empty, logger);
        if (string.IsNullOrWhiteSpace(userDataDir))
        {
            logger.Log(ComponentName, "Unable to prepare user data directory.");
            return false;
        }

        var arguments =
            $"--remote-debugging-port={DebugPort} " +
            $"--user-data-dir=\"{userDataDir}\" " +
            "--no-first-run " +
            "--no-default-browser-check " +
            "--autoplay-policy=no-user-gesture-required " +
            "--disable-session-crashed-bubble " +
            "--disable-infobars " +
            "--restore-last-session " +
            $"\"{StartupUrl}\"";

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

    private static string ResolveUserDataDirectory(string preferredDir, Logger logger)
    {
        var candidates = new[]
        {
            preferredDir,
            FallbackUserDataDir,
        };

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(candidate);
                return candidate;
            }
            catch (Exception ex)
            {
                logger.LogException(ComponentName, $"Failed preparing user data dir: {candidate}", ex);
            }
        }

        return string.Empty;
    }

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