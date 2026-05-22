// Launches Chrome with remote debugging enabled for YouTube automation.
// Resolves binary and user-data paths from config with safe fallbacks.
// Contains the ChromeManager class for Chrome startup orchestration.
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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

    private static IntPtr _previousForeground = IntPtr.Zero;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    private static void CaptureForegroundWindow()
    {
        try
        {
            _previousForeground = GetForegroundWindow();
        }
        catch
        {
            _previousForeground = IntPtr.Zero;
        }
    }

    private static bool RestoreForegroundWindow(Logger logger)
    {
        try
        {
            if (_previousForeground == IntPtr.Zero)
            {
                return false;
            }

            var current = GetForegroundWindow();
            if (current == _previousForeground)
            {
                return true;
            }

            // Try attaching input threads to allow SetForegroundWindow to succeed.
            var prevThread = GetWindowThreadProcessId(_previousForeground, out _);
            var currentThread = GetCurrentThreadId();

            var attached = false;
            try
            {
                attached = AttachThreadInput(currentThread, prevThread, true);
                // Restore if minimized
                ShowWindow(_previousForeground, SW_RESTORE);
                SetForegroundWindow(_previousForeground);

                // Check if succeeded
                var after = GetForegroundWindow();
                return after == _previousForeground;
            }
            finally
            {
                if (attached)
                {
                    AttachThreadInput(currentThread, prevThread, false);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogException(ComponentName, "RestoreForegroundWindow failed", ex);
            return false;
        }
    }

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

    private const string PreferredUserDataDir = @"C:\YouTube_User_Data";
    private const string LegacyGrid3UserDataDir = @"C:\Grid3_YouTube_Accessibility_Addon_User_Data";
    private const string LegacyUserDataDirV5 = @"C:\YouTube_User_Data_V5";

    /// <summary>
    /// Launches Chrome with the configured debugging profile.
    /// </summary>
    /// <param name="logger">The logger used for startup diagnostics.</param>
    /// <returns><see langword="true" /> when launch succeeds; otherwise, <see langword="false" />.</returns>
    public static bool Launch(Logger logger)
    {
        CaptureForegroundWindow();

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
            "--force-device-scale-factor=1 " +
            "--disable-features=Translate,SidePanel,ContentsCodeCache " +
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

            // Restore previous foreground window shortly after launch so the user's grid regains focus.
            Task.Run(() =>
            {
                try
                {
                    const int attempts = 8;
                    const int delayMs = 250;
                    const int stabilityCheckDelayMs = 120;
                    var stableSuccessAttempt = 0;

                    for (var attempt = 0; attempt < attempts; attempt++)
                    {
                        Thread.Sleep(delayMs);

                        if (!RestoreForegroundWindow(logger))
                        {
                            continue;
                        }

                        // Guard against early success by requiring foreground stability.
                        Thread.Sleep(stabilityCheckDelayMs);
                        if (IsPreviousWindowForeground())
                        {
                            stableSuccessAttempt = attempt + 1;
                            break;
                        }
                    }

                    if (stableSuccessAttempt > 0)
                    {
                        logger.Log(ComponentName, $"Foreground restore sequence completed successfully on attempt {stableSuccessAttempt}.");
                    }
                    else
                    {
                        logger.Log(ComponentName, "Foreground restore sequence did not restore the previous window.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogException(ComponentName, "Failed restoring previous foreground window", ex);
                }
            });

            return true;
        }
        catch (Exception ex)
        {
            logger.LogException(ComponentName, "Failed to launch Chrome", ex);
            return false;
        }
    }

    private static bool IsPreviousWindowForeground()
    {
        try
        {
            return _previousForeground != IntPtr.Zero && GetForegroundWindow() == _previousForeground;
        }
        catch
        {
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
        var migrationCandidates = new[]
        {
            LegacyGrid3UserDataDir,
            LegacyUserDataDirV5,
        };

        return UserDataDirectoryPolicy.Resolve(
            ComponentName,
            PreferredUserDataDir,
            migrationCandidates,
            logger);
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