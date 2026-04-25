// Represents persisted configuration used by YouTubeControl startup.
// Loads optional values from config.json and falls back to defaults.
// Contains the AppConfig model and config parsing helpers.
using System.Text.Json;

namespace YouTubeControl.Models;

/// <summary>
/// Stores runtime configuration for Chrome startup behavior.
/// </summary>
internal sealed class AppConfig
{
    /// <summary>
    /// Gets or sets the Chrome executable path override.
    /// </summary>
    public string? ChromePath { get; set; }

    /// <summary>
    /// Gets or sets the user-data directory override.
    /// </summary>
    public string? UserDataDir { get; set; }

    /// <summary>
    /// Loads configuration from disk or returns defaults when unavailable.
    /// </summary>
    /// <param name="configPath">The file path of config.json.</param>
    /// <param name="logger">The logger used for parse and I/O failures.</param>
    /// <returns>An initialized configuration object with safe fallback values.</returns>
    public static AppConfig LoadOrDefault(string configPath, Logger logger)
    {
        var fallback = CreateDefault();

        try
        {
            if (!File.Exists(configPath))
            {
                return fallback;
            }

            var json = File.ReadAllText(configPath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            string? chromePath = null;
            string? userDataDir = null;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("chromePath", out var chromePathNode) && chromePathNode.ValueKind == JsonValueKind.String)
                {
                    chromePath = chromePathNode.GetString();
                }

                if (root.TryGetProperty("userDataDir", out var userDataDirNode) && userDataDirNode.ValueKind == JsonValueKind.String)
                {
                    userDataDir = userDataDirNode.GetString();
                }
            }

            return new AppConfig
            {
                ChromePath = string.IsNullOrWhiteSpace(chromePath) ? fallback.ChromePath : chromePath,
                UserDataDir = string.IsNullOrWhiteSpace(userDataDir) ? fallback.UserDataDir : userDataDir,
            };
        }
        catch (Exception ex)
        {
            logger.LogException("AppConfig", "Failed to read config.json, using defaults", ex);
            return fallback;
        }
    }

    /// <summary>
    /// Creates the default configuration values.
    /// </summary>
    /// <returns>An AppConfig instance populated with default Chrome and profile paths.</returns>
    public static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            ChromePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google",
                "Chrome SxS",
                "Application",
                "chrome.exe"),
            UserDataDir = @"C:\YouTube_User_Data",
        };
    }
}