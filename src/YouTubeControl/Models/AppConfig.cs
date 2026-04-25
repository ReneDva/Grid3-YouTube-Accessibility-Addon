using System.Text.Json;

namespace YouTubeControl.Models;

internal sealed class AppConfig
{
    public string? ChromePath { get; set; }

    public string? UserDataDir { get; set; }

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
            UserDataDir = @"C:\YouTube_User_Data_V5",
        };
    }
}