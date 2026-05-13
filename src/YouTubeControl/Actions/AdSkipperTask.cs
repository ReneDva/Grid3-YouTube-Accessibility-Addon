// Runs in-process ad skip checks against active YouTube pages.
// Ports the browser-side skip button script from the V6 skipper implementation.
// Contains AdSkipperTask helpers for click detection and execution.
using System.Text.Json;
using PuppeteerSharp;

namespace YouTubeControl.Actions;

/// <summary>
/// Provides ad-skip execution helpers for leader background polling.
/// </summary>
internal static class AdSkipperTask
{
    private const string ComponentName = "AdSkipperTask";

    /// <summary>
    /// Poll interval used by the leader ad-skipper loop.
    /// </summary>
    internal static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(1500);

    /// <summary>
    /// Browser-side script ported from V6 skip_ads_cdp_V6.js.
    /// Returns JSON payload describing whether a clickable skip target was found.
    /// </summary>
    private const string BrowserSideScript = """
        (() => {
          function isVisible(el) {
            if (!el) return false;
            const style = window.getComputedStyle(el);
            const rect = el.getBoundingClientRect();
            return (
              style.display !== 'none' &&
              style.visibility !== 'hidden' &&
              parseFloat(style.opacity) > 0.5 &&
              rect.width > 5 &&
              rect.height > 5 &&
              rect.left >= 0
            );
          }

          const skipSelectors = [
            '.ytp-skip-ad-button',
            '.ytp-ad-skip-button-modern',
            '.ytp-ad-skip-button',
            '.ytp-ad-skip-button-container',
            '.ytp-skip-ad-button__text'
          ];

          for (const selector of skipSelectors) {
            const element = document.querySelector(selector);
            if (element && isVisible(element)) {
              const rect = element.getBoundingClientRect();
              return JSON.stringify({
                found: true,
                status: 'Skip Found',
                x: Math.round(rect.left + rect.width / 2),
                y: Math.round(rect.top + rect.height / 2)
              });
            }
          }

          const closeAd = document.querySelector('.ytp-ad-overlay-close-button');
          if (closeAd && isVisible(closeAd)) {
            const rect = closeAd.getBoundingClientRect();
            return JSON.stringify({
              found: true,
              status: 'Close Found',
              x: Math.round(rect.left + rect.width / 2),
              y: Math.round(rect.top + rect.height / 2)
            });
          }

          return JSON.stringify({ found: false });
        })()
        """;

    /// <summary>
    /// Attempts one skip-button evaluation and click operation.
    /// </summary>
    /// <param name="page">The active YouTube page.</param>
    /// <param name="logger">Logger for click-only action entries.</param>
    /// <param name="cancellationToken">Cancellation token from leader shutdown flow.</param>
    /// <returns><see langword="true"/> when a skip target was clicked; otherwise <see langword="false"/>.</returns>
    internal static async Task<bool> TrySkipAsync(IPage page, Logger logger, CancellationToken cancellationToken)
    {
        if (page.IsClosed || cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        try
        {
            var rawResult = await page.EvaluateExpressionAsync<string>(BrowserSideScript).ConfigureAwait(false);
            if (!TryParseSkipResult(rawResult, out var x, out var y, out var status))
            {
                return false;
            }

            await page.Mouse.ClickAsync(x, y).ConfigureAwait(false);
            logger.Log(ComponentName, $"Ad skip click executed: {status} at {x},{y}");
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (TargetClosedException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseSkipResult(string? rawResult, out decimal x, out decimal y, out string status)
    {
        x = 0;
        y = 0;
        status = string.Empty;

        if (string.IsNullOrWhiteSpace(rawResult))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(rawResult);
            var root = document.RootElement;

            var found = root.TryGetProperty("found", out var foundElement) && foundElement.GetBoolean();
            if (!found)
            {
                return false;
            }

            if (!root.TryGetProperty("x", out var xElement) ||
                !root.TryGetProperty("y", out var yElement) ||
                !root.TryGetProperty("status", out var statusElement))
            {
                return false;
            }

            x = xElement.GetDecimal();
            y = yElement.GetDecimal();
            status = statusElement.GetString() ?? "Skip Found";
            return true;
        }
        catch
        {
            return false;
        }
    }
}