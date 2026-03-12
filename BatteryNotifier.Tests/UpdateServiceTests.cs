using System.Reflection;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

/// <summary>
/// Tests for UpdateService version parsing and data model validation.
/// Network-dependent tests (actual GitHub API calls) are excluded.
/// </summary>
public class UpdateServiceTests
{
    // Access the private ParseVersion method via reflection
    private static Version? ParseVersion(string versionString)
    {
        var method = typeof(UpdateService).GetMethod("ParseVersion",
            BindingFlags.NonPublic | BindingFlags.Static);
        return (Version?)method!.Invoke(null, [versionString]);
    }

    [Theory]
    [InlineData("3.2.0", 3, 2, 0)]
    [InlineData("1.0.0", 1, 0, 0)]
    [InlineData("10.20.30", 10, 20, 30)]
    public void ParseVersion_ValidVersion_ReturnsCorrectParts(string input, int major, int minor, int build)
    {
        var version = ParseVersion(input);
        Assert.NotNull(version);
        Assert.Equal(major, version!.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(build, version.Build);
    }

    [Theory]
    [InlineData("v3.2.0")]
    [InlineData("V3.2.0")]
    [InlineData("v1.0.0")]
    public void ParseVersion_LeadingV_IsStripped(string input)
    {
        var version = ParseVersion(input);
        Assert.NotNull(version);
    }

    [Theory]
    [InlineData(" 3.2.0 ")]
    [InlineData("v 3.2.0")]
    public void ParseVersion_Whitespace_IsTrimmed(string input)
    {
        var version = ParseVersion(input);
        Assert.NotNull(version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-version")]
    [InlineData("abc")]
    [InlineData("v")]
    public void ParseVersion_InvalidVersion_ReturnsNull(string input)
    {
        var version = ParseVersion(input);
        Assert.Null(version);
    }

    [Fact]
    public void ParseVersion_VersionComparison_Works()
    {
        var v1 = ParseVersion("3.2.0");
        var v2 = ParseVersion("3.2.1");
        var v3 = ParseVersion("4.0.0");

        Assert.True(v2 > v1);
        Assert.True(v3 > v2);
        Assert.False(v1 > v2);
    }

    [Fact]
    public void ParseVersion_EqualVersions_AreEqual()
    {
        var v1 = ParseVersion("3.2.0");
        var v2 = ParseVersion("v3.2.0");

        Assert.Equal(v1, v2);
    }

    // ── Data Model Tests ────────────────────────────────────────────

    [Fact]
    public void GitHubRelease_DefaultValues()
    {
        var release = new GitHubRelease();
        Assert.Equal("", release.TagName);
        Assert.Equal("", release.Name);
        Assert.Equal("", release.Body);
        Assert.Equal("", release.HtmlUrl);
        Assert.Null(release.PublishedAt);
        Assert.Empty(release.Assets);
    }

    [Fact]
    public void UpdateCheckResult_PropertiesSetCorrectly()
    {
        var release = new GitHubRelease { TagName = "v3.3.0" };
        var result = new UpdateCheckResult(release, CheckStatus.UpdateAvailable);

        Assert.Equal(CheckStatus.UpdateAvailable, result.Status);
        Assert.NotNull(result.Release);
        Assert.Equal("v3.3.0", result.Release!.TagName);
    }

    [Fact]
    public void UpdateCheckResult_NullRelease_Works()
    {
        var result = new UpdateCheckResult(null, CheckStatus.UpToDate);
        Assert.Null(result.Release);
        Assert.Equal(CheckStatus.UpToDate, result.Status);
    }

    [Fact]
    public void UpdateAvailableEventArgs_StoresRelease()
    {
        var release = new GitHubRelease { TagName = "v4.0.0", Name = "Major release" };
        var args = new UpdateAvailableEventArgs(release);

        Assert.Same(release, args.Release);
        Assert.Equal("v4.0.0", args.Release.TagName);
    }

    [Fact]
    public void CheckStatus_AllValuesExist()
    {
        var values = Enum.GetValues<CheckStatus>();
        Assert.Contains(CheckStatus.UpToDate, values);
        Assert.Contains(CheckStatus.UpdateAvailable, values);
        Assert.Contains(CheckStatus.Failed, values);
        Assert.Contains(CheckStatus.AlreadyChecking, values);
    }
}
