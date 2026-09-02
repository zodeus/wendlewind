using Wendlemire.NetCode;
using Xunit;

namespace Wendlemire.Tests;

public class GameVersionTests
{
    [Fact]
    public void MatchesExactCurrentVersion()
    {
        Assert.True(GameVersion.Matches(GameVersion.Current));
        Assert.True(GameVersion.Matches($" {GameVersion.Current} "));
        Assert.False(GameVersion.Matches(""));
        Assert.False(GameVersion.Matches(null));
        Assert.False(GameVersion.Matches(GameVersion.Current + ".1"));
    }

    [Theory]
    [InlineData("/health", false)]
    [InlineData("/activate", false)]
    [InlineData("/downloads", false)]
    [InlineData("/download/win-x64", false)]
    [InlineData("/admin", false)]
    [InlineData("/admin/api/overview", false)]
    [InlineData("/favicon.ico", false)]
    [InlineData("/index.html", false)]
    [InlineData("/", false)]
    [InlineData("/players", true)]
    [InlineData("/players/abc/arena", true)]
    [InlineData("/builds", true)]
    [InlineData("/matches", true)]
    [InlineData("/opponent", true)]
    [InlineData("/analytics/fights", true)]
    public void RequiresClientVersionOnGameApisOnly(string path, bool required)
    {
        Assert.Equal(required, GameVersion.RequiresClientVersion(path));
    }

    [Fact]
    public void CoalesceKeepsFirstKnownVersion()
    {
        Assert.Equal("0.2", GameVersion.Coalesce(null, " 0.2 ", "0.3"));
        Assert.Equal(GameVersion.Current, GameVersion.Coalesce(null, "", " "));
    }

    [Fact]
    public void MismatchMessageTellsPlayerToDownloadLatestClient()
    {
        var message = GameVersion.MismatchMessage("0.1", "0.2");
        Assert.Contains("v0.1", message);
        Assert.Contains("v0.2", message);
        Assert.Contains(GameVersion.DownloadUrl, message);
        Assert.Contains("Download the latest client", message);
    }
}
