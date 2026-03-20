using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class PowerUsageServiceTests
{
    [Fact]
    public void ParsePsOutput_NormalOutput_ParsesCorrectly()
    {
        var output = """
            PID  %CPU COMMAND
              123  42.3 /Applications/Google Chrome.app/Contents/MacOS/Google Chrome
              456  15.1 /Applications/Slack.app/Contents/MacOS/Slack
              789  11.0 /Applications/Spotify.app/Contents/MacOS/Spotify
             1234   8.2 /Applications/Visual Studio Code.app/Contents/MacOS/Electron
             5678   3.5 /usr/bin/Terminal
            """;

        var result = PowerUsageService.ParsePsOutput(output);

        Assert.Equal(5, result.Count);
        Assert.Equal("Google Chrome", result[0].Name);
        Assert.Equal(42.3, result[0].CpuPercent);
        Assert.Equal(123, result[0].Pid);
        Assert.Equal("Slack", result[1].Name);
        Assert.Equal(15.1, result[1].CpuPercent);
        Assert.Equal("Terminal", result[4].Name);
        Assert.Equal(3.5, result[4].CpuPercent);
    }

    [Fact]
    public void ParsePsOutput_EmptyOutput_ReturnsEmpty()
    {
        var result = PowerUsageService.ParsePsOutput("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParsePsOutput_HeaderOnly_ReturnsEmpty()
    {
        var result = PowerUsageService.ParsePsOutput("  PID  %CPU COMMAND\n");
        Assert.Empty(result);
    }

    [Fact]
    public void ParsePsOutput_MalformedLines_SkipsGracefully()
    {
        var output = """
            PID  %CPU COMMAND
            abc  42.3 /usr/bin/foo
              123  xyz /usr/bin/bar
              456  10.5
              789  15.0 /usr/bin/valid
            """;

        var result = PowerUsageService.ParsePsOutput(output);

        Assert.Single(result);
        Assert.Equal("valid", result[0].Name);
        Assert.Equal(15.0, result[0].CpuPercent);
    }

    [Fact]
    public void ParsePsOutput_ProcessNameWithSpaces_ParsesCorrectly()
    {
        var output = """
            PID  %CPU COMMAND
              100  25.0 /Applications/Google Chrome Helper (GPU).app/Contents/MacOS/Google Chrome Helper (GPU)
              200  10.0 /usr/bin/simple
            """;

        var result = PowerUsageService.ParsePsOutput(output);

        Assert.Equal(2, result.Count);
        Assert.Equal("Google Chrome Helper (GPU)", result[0].Name);
        Assert.Equal(25.0, result[0].CpuPercent);
    }

    [Fact]
    public void ParsePsOutput_ProcessWithoutPath_UsesFullName()
    {
        var output = """
            PID  %CPU COMMAND
              100  12.5 someprocess
            """;

        var result = PowerUsageService.ParsePsOutput(output);

        Assert.Single(result);
        Assert.Equal("someprocess", result[0].Name);
    }

    [Fact]
    public void ParsePsOutput_ZeroCpu_StillParsed()
    {
        var output = """
            PID  %CPU COMMAND
              100   0.0 /usr/bin/idle
              200   5.5 /usr/bin/active
            """;

        var result = PowerUsageService.ParsePsOutput(output);

        Assert.Equal(2, result.Count);
        Assert.Equal(0.0, result[0].CpuPercent);
        Assert.Equal(5.5, result[1].CpuPercent);
    }

    [Fact]
    public void ParsePsOutput_LinuxFormat_ParsesCorrectly()
    {
        var output = """
            PID  %CPU COMMAND
             1234  30.2 /usr/lib/firefox/firefox
              567   8.0 /usr/bin/code
            """;

        var result = PowerUsageService.ParsePsOutput(output);

        Assert.Equal(2, result.Count);
        Assert.Equal("firefox", result[0].Name);
        Assert.Equal(30.2, result[0].CpuPercent);
        Assert.Equal("code", result[1].Name);
    }
}
