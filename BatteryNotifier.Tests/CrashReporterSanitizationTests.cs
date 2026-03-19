using System.Reflection;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

/// <summary>
/// Tests CrashReporter's content sanitization, PII removal,
/// and markdown/HTML neutralization. Uses reflection to access
/// private static methods since they are the core security layer.
/// </summary>
public class CrashReporterSanitizationTests
{
    // Use reflection to access private static methods
    private static string SanitizeLine(string line) =>
        (string)typeof(CrashReporter)
            .GetMethod("SanitizeLine", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [line])!;

    private static string NeutralizeMarkdown(string text) =>
        (string)typeof(CrashReporter)
            .GetMethod("NeutralizeMarkdown", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [text])!;

    private static string SanitizeContent(string value) =>
        (string)typeof(CrashReporter)
            .GetMethod("SanitizeContent", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [value])!;

    // ── PII Removal ────────────────────────────────────────────────
    // Note: SanitizeLine runs PII regex replacement first, then NeutralizeMarkdown
    // which strips HTML-like tags. So placeholder text like <drive>, <user>, <redacted>
    // gets removed by the HTML stripping. The important thing is that usernames are gone.

    [Theory]
    [InlineData(@"Error at C:\Users\Alice\Projects\app.cs", "Alice")]
    [InlineData(@"File: C:\Users\Bob\Desktop\file.txt", "Bob")]
    [InlineData("/home/alice/projects/app.cs", "alice")]
    [InlineData("/Users/bob/Desktop/file.txt", "bob")]
    public void SanitizeLine_RemovesUsernames(string input, string username)
    {
        var result = SanitizeLine(input);
        Assert.DoesNotContain(username, result);
    }

    [Fact]
    public void SanitizeLine_RedactsMachineName()
    {
        var input = "MachineName\": \"MY-WORKSTATION\" something";
        var result = SanitizeLine(input);
        Assert.DoesNotContain("MY-WORKSTATION", result);
    }

    [Fact]
    public void SanitizeLine_RedactsAppId()
    {
        var input = "AppId\": \"12345678-1234-1234-1234-123456789abc\" data";
        var result = SanitizeLine(input);
        Assert.DoesNotContain("12345678-1234-1234-1234-123456789abc", result);
    }

    // ── Line Truncation ─────────────────────────────────────────────

    [Fact]
    public void SanitizeLine_LongLine_IsTruncated()
    {
        var longLine = new string('A', 600);
        var result = SanitizeLine(longLine);
        Assert.True(result.Length <= 510); // 500 + "…" + possible markdown neutralization
        Assert.EndsWith("…", result);
    }

    [Fact]
    public void SanitizeLine_ShortLine_NotTruncated()
    {
        var shortLine = "Short log entry";
        var result = SanitizeLine(shortLine);
        Assert.Equal(shortLine, result);
    }

    // ── Markdown Neutralization ──────────────────────────────────────

    [Fact]
    public void NeutralizeMarkdown_RemovesHtmlTags()
    {
        Assert.DoesNotContain("<script>", NeutralizeMarkdown("<script>alert(1)</script>"));
        Assert.DoesNotContain("<img", NeutralizeMarkdown("<img src=x onerror=alert(1)>"));
        Assert.DoesNotContain("<a", NeutralizeMarkdown("<a href='evil.com'>click</a>"));
    }

    [Fact]
    public void NeutralizeMarkdown_BreaksMarkdownLinks()
    {
        var result = NeutralizeMarkdown("[Click me](https://evil.com)");
        Assert.Contains("⟦Click me⟧", result);
        // Should not render as a clickable link
        Assert.DoesNotContain("[Click me]", result);
    }

    [Fact]
    public void NeutralizeMarkdown_BreaksMarkdownImages()
    {
        // Image syntax ![alt](url) should be neutralized.
        // The image regex runs BEFORE the link regex, so it produces ⟦img:alt text⟧(url).
        // But then the URL inside (https://evil.com/...) gets broken by the bare URL regex.
        var result = NeutralizeMarkdown("![alt text](https://evil.com/image.png)");
        // The ![...] brackets should be neutralized
        Assert.DoesNotContain("![alt text]", result);
    }

    [Fact]
    public void NeutralizeMarkdown_BreaksExternalUrls()
    {
        var result = NeutralizeMarkdown("Visit https://attacker.com/payload");
        // Should contain zero-width space to break auto-linking
        Assert.Contains("\u200B", result);
    }

    [Fact]
    public void NeutralizeMarkdown_AllowsTrustedRepoUrl()
    {
        var repoUrl = "https://github.com/Sandip124/BatteryNotifier/issues/123";
        var result = NeutralizeMarkdown($"See {repoUrl}");
        // Trusted URL should NOT be broken
        Assert.Contains(repoUrl, result);
    }

    [Fact]
    public void NeutralizeMarkdown_PlainText_Unchanged()
    {
        var text = "Normal log line with no special chars 2024-01-15 10:30:00";
        Assert.Equal(text, NeutralizeMarkdown(text));
    }

    // ── SanitizeContent ──────────────────────────────────────────────

    [Fact]
    public void SanitizeContent_Empty_ReturnsEmpty()
    {
        Assert.Equal("", SanitizeContent(""));
    }

    [Fact]
    public void SanitizeContent_Null_ReturnsEmpty()
    {
        var result = (string)typeof(CrashReporter)
            .GetMethod("SanitizeContent", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [null!])!;
        Assert.Equal("", result);
    }

    [Fact]
    public void SanitizeContent_LongValue_Truncated()
    {
        var longValue = new string('X', 600);
        var result = SanitizeContent(longValue);
        Assert.True(result.Length <= 510);
    }

    // ── Report Building ──────────────────────────────────────────────

    [Fact]
    public void BuildManualReport_ContainsExpectedSections()
    {
        var report = CrashReporter.BuildManualReport();

        Assert.Contains("Manual Log Report", report);
        Assert.Contains("System Info", report);
        Assert.Contains("Version:", report);
        Assert.Contains("OS:", report);
        Assert.Contains("Runtime:", report);
    }

    [Fact]
    public void BuildCrashReport_SanitizesCrashDetails()
    {
        var crashDetails = "Error at C:\\Users\\SecretUser\\project\\file.cs\n" +
            "MachineName\": \"SECRET-PC\"\n" +
            "<script>alert('xss')</script>";

        var report = CrashReporter.BuildCrashReport(crashDetails);

        Assert.DoesNotContain("SecretUser", report);
        Assert.DoesNotContain("SECRET-PC", report);
        Assert.DoesNotContain("<script>", report);
        Assert.Contains("Crash Report", report);
    }

    // ── SaveReportToFile ──────────────────────────────────────────────

    [Fact]
    public void SaveReportToFile_CreatesFile()
    {
        var report = "Test report content";
        var path = CrashReporter.SaveReportToFile(report);

        try
        {
            Assert.False(string.IsNullOrEmpty(path));
            Assert.True(File.Exists(path));

            var content = File.ReadAllText(path);
            Assert.Equal(report, content);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
