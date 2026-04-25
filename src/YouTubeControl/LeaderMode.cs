// Hosts leader-side pipe listening for incoming messenger commands.
// Runs an async loop with cancellation and timeout-aware accepts.
// Contains the LeaderMode class for leader process command intake.
using System.IO.Pipes;
using System.Text;
using PuppeteerSharp;
using YouTubeControl.Actions;

namespace YouTubeControl;

/// <summary>
/// Runs the leader-side named pipe loop for command intake.
/// </summary>
/// <remarks>
/// Accepts one connection per loop iteration, reads a single command line, and logs results
/// until <paramref name="cancellationToken" /> is canceled.
/// </remarks>
internal static class LeaderMode
{
    private const string ComponentName = "LeaderMode";
    private const string HomeUrl = "https://www.youtube.com";
    private static readonly SemaphoreSlim BrowserLock = new(1, 1);
    private static volatile bool _exitRequested;

    private static readonly HashSet<string> SupportedActions =
    [
        "home", "up", "down", "enter", "back", "play_pause", "fullscreen", "toggle", "like", "search", "open", "exit", "refresh",
    ];

    private static IBrowser? _browser;

    /// <summary>
    /// Starts the leader pipe loop on a background task.
    /// </summary>
    /// <param name="logger">The logger used for command and error events.</param>
    /// <param name="cancellationToken">The cancellation token for graceful shutdown.</param>
    /// <returns>A task that represents the lifetime of the leader loop.</returns>
    public static async Task RunAsync(Logger logger, CancellationToken cancellationToken)
    {
        _exitRequested = false;

        var startupBrowser = await EnsureBrowserConnectedAsync(
            logger,
            cancellationToken,
            allowLaunchIfUnavailable: true).ConfigureAwait(false);

        if (startupBrowser is null)
        {
            logger.Log(ComponentName, "Leader startup aborted because Chrome attach/launch bootstrap failed.");
            throw new InvalidOperationException("Chrome bootstrap attach/launch failed.");
        }

        var pipeTask = RunPipeServerLoopAsync(logger, cancellationToken);
        var recoveryTask = RunCdpRecoveryLoopAsync(logger, cancellationToken);

        await Task.WhenAll(pipeTask, recoveryTask).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the pipe server accept-read loop until cancellation is requested.
    /// </summary>
    /// <param name="logger">The logger used for command and error events.</param>
    /// <param name="cancellationToken">The cancellation token for graceful shutdown.</param>
    private static async Task RunPipeServerLoopAsync(Logger logger, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Open one server instance per accepted command.
                using var server = new NamedPipeServerStream(
                    Program.PipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                // Stop waiting after a bounded accept window.
                using var acceptTimeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                acceptTimeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

                await server.WaitForConnectionAsync(acceptTimeoutCts.Token).ConfigureAwait(false);

                // Read one command line from the connected client.
                using var reader = new StreamReader(server, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                var command = await reader.ReadLineAsync().ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(command))
                {
                    logger.Log(ComponentName, $"Leader received command: {command}");
                    await DispatchCommandAsync(command, logger, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.Log(ComponentName, "Leader received an empty command.");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown or timed-out accept.
            }
            catch (Exception ex)
            {
                logger.LogException(ComponentName, "Leader pipe loop error", ex);
            }
        }

        logger.Log(ComponentName, "Leader pipe loop stopped.");
    }

    private static async Task RunCdpRecoveryLoopAsync(Logger logger, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await EnsureBrowserConnectedAsync(
                    logger,
                    cancellationToken,
                    allowLaunchIfUnavailable: false).ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown.
            }
            catch (Exception ex)
            {
                logger.LogException(ComponentName, "CDP recovery loop error", ex);
            }
        }
    }

    private static async Task DispatchCommandAsync(string rawCommand, Logger logger, CancellationToken cancellationToken)
    {
        if (!TryParseCommand(rawCommand, out var action, out var query))
        {
            logger.Log(ComponentName, $"Rejected invalid command: {rawCommand}");
            return;
        }

        if (!SupportedActions.Contains(action))
        {
            logger.Log(ComponentName, $"Rejected unsupported action: {action}");
            return;
        }

        if (action == "exit")
        {
            _exitRequested = true;
            await CloseBrowserAsync(logger).ConfigureAwait(false);
            logger.Log(ComponentName, "Exit command received. Browser tabs and window were closed.");
            return;
        }

        _exitRequested = false;

        if (action == "refresh")
        {
            var pageForRefresh = await GetYouTubePageAsync(
                logger,
                cancellationToken,
                allowLaunchIfUnavailable: false).ConfigureAwait(false);

            if (pageForRefresh is null)
            {
                logger.Log(ComponentName, "Refresh ignored because no active YouTube tab was found.");
                return;
            }

            await pageForRefresh.BringToFrontAsync().ConfigureAwait(false);
            await pageForRefresh.ReloadAsync().ConfigureAwait(false);
            logger.Log(ComponentName, "Refresh command executed.");
            return;
        }

        var allowLaunchIfUnavailable = action is "home" or "open" or "search";
        var page = await GetYouTubePageAsync(
            logger,
            cancellationToken,
            allowLaunchIfUnavailable).ConfigureAwait(false);

        if (page is null)
        {
            logger.Log(ComponentName, "No browser page available for command dispatch.");
            return;
        }

        if (!allowLaunchIfUnavailable && !IsYouTubeUrl(page.Url))
        {
            logger.Log(ComponentName, $"Action '{action}' ignored because no YouTube tab is open.");
            return;
        }

        if (!await TryBringToFrontAsync(page, logger).ConfigureAwait(false))
        {
            logger.Log(ComponentName, "Command ignored because target page is no longer available.");
            return;
        }

        var actionForScript = action;

        switch (action)
        {
            case "home":
                await page.GoToAsync(HomeUrl).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken).ConfigureAwait(false);
                actionForScript = "open";
                break;
            case "open" when !string.IsNullOrWhiteSpace(query):
                await page.GoToAsync(query).ConfigureAwait(false);
                break;
            case "search" when !string.IsNullOrWhiteSpace(query):
                await page.GoToAsync($"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}").ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(2500), cancellationToken).ConfigureAwait(false);
                break;
            case "back":
                await page.GoBackAsync().ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
                break;
        }

        var navScript = NavigationActions.BuildNavScript(actionForScript);
        var result = await page.EvaluateExpressionAsync<string>(navScript).ConfigureAwait(false);
        logger.Log(ComponentName, $"Action '{actionForScript}' executed with result: {result}");
    }

    private static bool TryParseCommand(string rawCommand, out string action, out string query)
    {
        action = string.Empty;
        query = string.Empty;

        if (string.IsNullOrWhiteSpace(rawCommand) || rawCommand.Length > 512)
        {
            return false;
        }

        var firstColon = rawCommand.IndexOf(':');
        if (firstColon < 0)
        {
            action = rawCommand.Trim().ToLowerInvariant();
            return !string.IsNullOrWhiteSpace(action);
        }

        action = rawCommand[..firstColon].Trim().ToLowerInvariant();
        query = rawCommand[(firstColon + 1)..].Trim();
        return !string.IsNullOrWhiteSpace(action);
    }

    private static async Task<IPage?> GetYouTubePageAsync(
        Logger logger,
        CancellationToken cancellationToken,
        bool allowLaunchIfUnavailable)
    {
        var browser = await EnsureBrowserConnectedAsync(
            logger,
            cancellationToken,
            allowLaunchIfUnavailable).ConfigureAwait(false);

        if (browser is null)
        {
            return null;
        }

        try
        {
            var pages = await browser.PagesAsync().ConfigureAwait(false);
            var openPages = pages.Where(p => !p.IsClosed).ToList();

            var youtubePage = openPages.FirstOrDefault(p => IsYouTubeUrl(p.Url));
            if (youtubePage is not null)
            {
                return youtubePage;
            }

            if (!allowLaunchIfUnavailable)
            {
                return null;
            }

            if (openPages.Count > 0)
            {
                return openPages[0];
            }

            return await browser.NewPageAsync().ConfigureAwait(false);
        }
        catch (TargetClosedException)
        {
            await InvalidateBrowserAsync().ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogException(ComponentName, "Failed to resolve active browser page", ex);
            return null;
        }
    }

    private static bool IsYouTubeUrl(string? url)
    {
        return !string.IsNullOrWhiteSpace(url) &&
            url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<IBrowser?> EnsureBrowserConnectedAsync(
        Logger logger,
        CancellationToken cancellationToken,
        bool allowLaunchIfUnavailable)
    {
        if (_browser is { IsConnected: true })
        {
            return _browser;
        }

        await BrowserLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_browser is { IsConnected: true })
            {
                return _browser;
            }

            await InvalidateBrowserAsync().ConfigureAwait(false);

            if (await TryConnectWithBackoffAsync(logger, cancellationToken, maxAttempts: 3).ConfigureAwait(false))
            {
                return _browser;
            }

            if (!allowLaunchIfUnavailable || _exitRequested)
            {
                return null;
            }

            if (!ChromeManager.Launch(logger))
            {
                return null;
            }

            if (await TryConnectWithBackoffAsync(logger, cancellationToken, maxAttempts: 5).ConfigureAwait(false))
            {
                return _browser;
            }

            return null;
        }
        finally
        {
            BrowserLock.Release();
        }
    }

    private static async Task<bool> TryConnectWithBackoffAsync(
        Logger logger,
        CancellationToken cancellationToken,
        int maxAttempts)
    {
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            try
            {
                _browser = await Puppeteer.ConnectAsync(new ConnectOptions
                {
                    BrowserURL = ChromeManager.BrowserUrl,
                }).ConfigureAwait(false);

                logger.Log(ComponentName, $"Connected to Chrome CDP at {ChromeManager.BrowserUrl}.");
                return true;
            }
            catch (Exception ex)
            {
                var delayMs = (int)(500 * Math.Pow(2, attempt));
                logger.LogException(ComponentName, $"CDP connect attempt {attempt + 1} failed", ex);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    /// <summary>
    /// Brings the target page to the foreground when the page/session is still valid.
    /// </summary>
    /// <param name="page">The candidate page to focus.</param>
    /// <param name="logger">The logger used for focus failures.</param>
    /// <returns><see langword="true"/> when the page was focused; otherwise <see langword="false"/>.</returns>
    private static async Task<bool> TryBringToFrontAsync(IPage page, Logger logger)
    {
        try
        {
            if (page.IsClosed)
            {
                await InvalidateBrowserAsync().ConfigureAwait(false);
                return false;
            }

            await page.BringToFrontAsync().ConfigureAwait(false);
            return true;
        }
        catch (TargetClosedException)
        {
            await InvalidateBrowserAsync().ConfigureAwait(false);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogException(ComponentName, "Failed to bring page to front", ex);
            return false;
        }
    }

    private static async Task CloseBrowserAsync(Logger logger)
    {
        try
        {
            var browser = _browser;
            if (browser is null || !browser.IsConnected)
            {
                browser = await EnsureBrowserConnectedAsync(
                    logger,
                    CancellationToken.None,
                    allowLaunchIfUnavailable: false).ConfigureAwait(false);
            }

            if (browser is null)
            {
                logger.Log(ComponentName, "Exit command could not find an attached browser instance.");
                return;
            }

            IReadOnlyList<IPage> pages;
            try
            {
                pages = await browser.PagesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogException(ComponentName, "Failed to query browser pages during exit", ex);
                pages = [];
            }

            foreach (var page in pages)
            {
                try
                {
                    await page.CloseAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogException(ComponentName, "Failed to close browser tab during exit", ex);
                }
            }

            try
            {
                await browser.CloseAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogException(ComponentName, "Failed to close browser window during exit", ex);
            }

            await InvalidateBrowserAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogException(ComponentName, "Failed closing browser", ex);
        }
    }

    private static Task InvalidateBrowserAsync()
    {
        var browser = _browser;
        _browser = null;

        if (browser is null)
        {
            return Task.CompletedTask;
        }

        try
        {
            browser.Disconnect();
        }
        catch
        {
            // Ignore invalidation failures.
        }

        return Task.CompletedTask;
    }
}
