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
