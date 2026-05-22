using System.Diagnostics.CodeAnalysis;

namespace YouTubeControl;

internal static class UserDataDirectoryPolicy
{
    internal static string Resolve(
        string componentName,
        string preferredDirectory,
        IReadOnlyList<string> migrationCandidates,
        Logger logger)
    {
        try
        {
            Directory.CreateDirectory(preferredDirectory);

            if (HasProfileData(preferredDirectory))
            {
                logger.Log(componentName, $"Using preferred Chrome user-data directory: {preferredDirectory}");
                return preferredDirectory;
            }

            foreach (var candidate in migrationCandidates)
            {
                if (TryMigrateLegacyProfile(componentName, candidate, preferredDirectory, logger) && HasProfileData(preferredDirectory))
                {
                    logger.Log(componentName, $"Migrated profile from legacy directory: {candidate}");
                    return preferredDirectory;
                }
            }

            // Fallback to first legacy path with profile data if migration failed for any reason.
            foreach (var candidate in migrationCandidates)
            {
                if (HasProfileData(candidate))
                {
                    logger.Log(componentName, $"Migration skipped; using legacy profile directory for this run: {candidate}");
                    return candidate;
                }
            }

            logger.Log(componentName, $"First install profile bootstrap at: {preferredDirectory}");
            return preferredDirectory;
        }
        catch (Exception ex)
        {
            logger.LogException(componentName, $"Failed preparing user data dir: {preferredDirectory}", ex);
            return string.Empty;
        }
    }

    internal static bool HasProfileData([NotNullWhen(true)] string? directory)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return false;
            }

            var defaultProfileDir = Path.Combine(directory, "Default");
            if (!Directory.Exists(defaultProfileDir))
            {
                return false;
            }

            return File.Exists(Path.Combine(defaultProfileDir, "Login Data")) ||
                File.Exists(Path.Combine(defaultProfileDir, "Preferences"));
        }
        catch
        {
            return false;
        }
    }

    private static bool TryMigrateLegacyProfile(string componentName, string legacyDirectory, string targetDirectory, Logger logger)
    {
        if (!HasProfileData(legacyDirectory))
        {
            return false;
        }

        try
        {
            Directory.CreateDirectory(targetDirectory);
            CopyDirectoryRecursively(legacyDirectory, targetDirectory);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogException(componentName, $"Failed migrating legacy profile from {legacyDirectory} to {targetDirectory}", ex);
            return false;
        }
    }

    private static void CopyDirectoryRecursively(string sourceDirectory, string targetDirectory)
    {
        var source = new DirectoryInfo(sourceDirectory);
        var target = new DirectoryInfo(targetDirectory);

        if (!target.Exists)
        {
            target.Create();
        }

        foreach (var file in source.GetFiles())
        {
            var destinationPath = Path.Combine(target.FullName, file.Name);
            if (!File.Exists(destinationPath))
            {
                file.CopyTo(destinationPath, overwrite: false);
            }
        }

        foreach (var directory in source.GetDirectories())
        {
            var targetSubDirectory = Path.Combine(target.FullName, directory.Name);
            CopyDirectoryRecursively(directory.FullName, targetSubDirectory);
        }
    }
}
