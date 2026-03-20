using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class ProcessTipsTests
{
    [Theory]
    [InlineData("Google Chrome", "Close unused tabs to save battery")]
    [InlineData("Google Chrome Helper (GPU)", "Close unused tabs to save battery")]
    [InlineData("chromium-browser", "Close unused tabs to save battery")]
    [InlineData("firefox", "Close unused tabs to save battery")]
    [InlineData("Safari", "Close unused tabs to save battery")]
    [InlineData("Microsoft Edge", "Close unused tabs to save battery")]
    [InlineData("Brave Browser", "Close unused tabs to save battery")]
    public void GetTip_Browsers_ReturnTabTip(string name, string expectedTip)
    {
        Assert.Equal(expectedTip, ProcessTips.GetTip(name));
    }

    [Theory]
    [InlineData("Slack", "Quit when not actively messaging")]
    [InlineData("Slack Helper", "Quit when not actively messaging")]
    [InlineData("Discord", "Quit when not in a call")]
    [InlineData("Microsoft Teams", "Quit when not in a meeting")]
    [InlineData("zoom.us", "Quit after your meeting ends")]
    public void GetTip_Communication_ReturnQuitTip(string name, string expectedTip)
    {
        Assert.Equal(expectedTip, ProcessTips.GetTip(name));
    }

    [Theory]
    [InlineData("Spotify", "Download music instead of streaming")]
    [InlineData("mds_stores", "Spotlight indexing — will finish soon")]
    [InlineData("photoanalysisd", "Photo analysis — will finish soon")]
    [InlineData("Docker", "Pause unused containers or VMs")]
    public void GetTip_KnownApps_ReturnCorrectTip(string name, string expectedTip)
    {
        Assert.Equal(expectedTip, ProcessTips.GetTip(name));
    }

    [Theory]
    [InlineData("myCustomApp")]
    [InlineData("python3")]
    [InlineData("rust-analyzer")]
    public void GetTip_UnknownApps_ReturnNull(string name)
    {
        Assert.Null(ProcessTips.GetTip(name));
    }

    [Fact]
    public void GetTip_CaseInsensitive()
    {
        Assert.Equal("Close unused tabs to save battery", ProcessTips.GetTip("GOOGLE CHROME"));
        Assert.Equal("Quit when not actively messaging", ProcessTips.GetTip("SLACK"));
        Assert.Equal("Spotlight indexing — will finish soon", ProcessTips.GetTip("MDS_STORES"));
    }

    [Theory]
    [InlineData("Arc", "Close unused spaces or tabs")]
    [InlineData("Telegram", "Quit when not actively messaging")]
    [InlineData("WhatsApp", "Quit when not actively messaging")]
    [InlineData("node", "Check for runaway processes")]
    [InlineData("backupd", "Time Machine backup in progress")]
    public void GetTip_AdditionalApps(string name, string expectedTip)
    {
        Assert.Equal(expectedTip, ProcessTips.GetTip(name));
    }
}
