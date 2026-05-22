namespace YouTubeControl.Tests;

public class MessengerModeTests
{
    [Fact]
    public void BuildCommand_JoinsEnglishArguments()
    {
        var command = MessengerMode.BuildCommand(["search:lion", "king", "trailer"]);

        Assert.Equal("search:lion king trailer", command);
    }

    [Fact]
    public void BuildCommand_JoinsHebrewArguments()
    {
        var command = MessengerMode.BuildCommand(["search:מלך", "האריות"]);

        Assert.Equal("search:מלך האריות", command);
    }

    [Fact]
    public void BuildCommand_ReturnsNullForWhitespaceOnly()
    {
        var command = MessengerMode.BuildCommand(["   ", "  "]);

        Assert.Null(command);
    }
}

public class UserDataDirectoryPolicyTests
{
    [Fact]
    public void Resolve_UsesPreferredDirectory_WhenPreferredHasProfile()
    {
        var root = CreateTempRoot();
        try
        {
            var preferredDir = Path.Combine(root, "preferred");
            var legacyDir = Path.Combine(root, "legacy-grid3");
            var logger = new Logger(Path.Combine(root, "logs.txt"));

            CreateProfileData(preferredDir);

            var resolved = UserDataDirectoryPolicy.Resolve("Test", preferredDir, [legacyDir], logger);

            Assert.Equal(preferredDir, resolved);
            Assert.True(UserDataDirectoryPolicy.HasProfileData(preferredDir));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void Resolve_MigratesFromLegacy_WhenOnlyLegacyHasProfile()
    {
        var root = CreateTempRoot();
        try
        {
            var preferredDir = Path.Combine(root, "preferred");
            var legacyDir = Path.Combine(root, "legacy-grid3");
            var logger = new Logger(Path.Combine(root, "logs.txt"));

            CreateProfileData(legacyDir);
            File.WriteAllText(Path.Combine(legacyDir, "legacy-marker.txt"), "migrated");

            var resolved = UserDataDirectoryPolicy.Resolve("Test", preferredDir, [legacyDir], logger);

            Assert.Equal(preferredDir, resolved);
            Assert.True(UserDataDirectoryPolicy.HasProfileData(preferredDir));
            Assert.True(File.Exists(Path.Combine(preferredDir, "legacy-marker.txt")));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    [Fact]
    public void Resolve_BootstrapsPreferredDirectory_WhenNoProfileExists()
    {
        var root = CreateTempRoot();
        try
        {
            var preferredDir = Path.Combine(root, "preferred");
            var legacyDir = Path.Combine(root, "legacy-grid3");
            var logger = new Logger(Path.Combine(root, "logs.txt"));

            var resolved = UserDataDirectoryPolicy.Resolve("Test", preferredDir, [legacyDir], logger);

            Assert.Equal(preferredDir, resolved);
            Assert.True(Directory.Exists(preferredDir));
            Assert.False(UserDataDirectoryPolicy.HasProfileData(preferredDir));
        }
        finally
        {
            DeleteTempRoot(root);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "YouTubeControl.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTempRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static void CreateProfileData(string directory)
    {
        var defaultDir = Path.Combine(directory, "Default");
        Directory.CreateDirectory(defaultDir);
        File.WriteAllText(Path.Combine(defaultDir, "Preferences"), "{}");
    }
}
