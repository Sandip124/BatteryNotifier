using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Handles crash detection, log collection, and user-initiated error reporting.
///
/// Security:
/// - Rate limiting: max 1 report per hour (persisted to disk)
/// - Crash marker validation: HMAC signature prevents injection of fake crash reports
/// - Content sanitization: markdown/HTML stripped, PII removed, content length bounded
/// - Delivery: opens a pre-filled GitHub issue in the browser — user reviews before submitting
/// </summary>
public static class CrashReporter
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("CrashReporter");

    private const string CrashMarkerFileName = ".crash-report";
    private const string CrashMarkerSigFileName = ".crash-report.sig";
    private const string RateLimitFileName = ".last-report";
    private const string IssueRepoUrl = Constants.SourceRepositoryUrl;
    private const int MaxLogLines = 200;
    private const int MaxLogFileSize = 512 * 1024; // 512 KB per file
    private const int MaxCrashMarkerSize = 64 * 1024; // 64 KB
    private const int MaxLineLength = 500; // Truncate individual lines

    /// <summary>Minimum interval between reports (prevents spam).</summary>
    private static readonly TimeSpan ReportCooldown = TimeSpan.FromHours(1);

    private static string DataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatteryNotifier");

    private static string CrashMarkerPath => Path.Combine(DataDirectory, CrashMarkerFileName);
    private static string CrashMarkerSigPath => Path.Combine(DataDirectory, CrashMarkerSigFileName);
    private static string RateLimitPath => Path.Combine(DataDirectory, RateLimitFileName);

    // ── Rate Limiting ────────────────────────────────────────────

    /// <summary>
    /// Returns true if a report can be sent (cooldown has elapsed).
    /// </summary>
    public static bool CanSendReport()
    {
        try
        {
            if (!File.Exists(RateLimitPath)) return true;

            var lastReportText = File.ReadAllText(RateLimitPath).Trim();
            if (DateTime.TryParse(lastReportText, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var lastReport))
            {
                return (DateTime.UtcNow - lastReport) >= ReportCooldown;
            }

            return true; // Corrupted file — allow
        }
        catch
        {
            return true;
        }
    }

    /// <summary>
    /// Returns the remaining cooldown time, or TimeSpan.Zero if ready.
    /// </summary>
    public static TimeSpan GetCooldownRemaining()
    {
        try
        {
            if (!File.Exists(RateLimitPath)) return TimeSpan.Zero;

            var lastReportText = File.ReadAllText(RateLimitPath).Trim();
            if (DateTime.TryParse(lastReportText, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var lastReport))
            {
                var remaining = ReportCooldown - (DateTime.UtcNow - lastReport);
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
        catch { }

        return TimeSpan.Zero;
    }

    private static void RecordReportSent()
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            File.WriteAllText(RateLimitPath, DateTime.UtcNow.ToString("O"));
        }
        catch { }
    }

    // ── Crash Marker (HMAC-signed) ───────────────────────────────

    /// <summary>
    /// Write a crash marker file so the next launch knows a crash occurred.
    /// The marker is HMAC-signed with the settings key to prevent injection
    /// of fake crash reports by writing arbitrary files to the data directory.
    /// </summary>
    public static void WriteCrashMarker(Exception? exception)
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            var content = new StringBuilder();
            content.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
            content.AppendLine($"Version: {Constants.ApplicationVersion}");
            content.AppendLine($"OS: {RuntimeInformation.OSDescription}");
            content.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
            content.AppendLine();

            if (exception != null)
            {
                content.AppendLine("--- Exception ---");
                content.AppendLine(SanitizeException(exception));
            }

            var markerContent = content.ToString();
            File.WriteAllText(CrashMarkerPath, markerContent);

            // Sign the marker so we can verify it wasn't tampered with
            var signature = ComputeHmac(markerContent);
            File.WriteAllText(CrashMarkerSigPath, signature);
        }
        catch
        {
            // Best effort — we're already crashing
        }
    }

    /// <summary>
    /// Check if a crash marker exists from a previous session.
    /// Validates the HMAC signature to reject injected/tampered markers.
    /// Returns the crash details or null if no crash occurred.
    /// </summary>
    public static string? DetectPreviousCrash()
    {
        try
        {
            if (!File.Exists(CrashMarkerPath)) return null;

            // Size guard — reject suspiciously large markers
            var fileInfo = new FileInfo(CrashMarkerPath);
            if (fileInfo.Length > MaxCrashMarkerSize)
            {
                CleanupCrashMarker();
                return null;
            }

            var content = File.ReadAllText(CrashMarkerPath);

            // Verify HMAC signature — rejects injected or tampered markers
            if (File.Exists(CrashMarkerSigPath))
            {
                var expectedSig = File.ReadAllText(CrashMarkerSigPath).Trim();
                var actualSig = ComputeHmac(content);

                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expectedSig),
                        Encoding.UTF8.GetBytes(actualSig)))
                {
                    CleanupCrashMarker();
                    return null;
                }
            }
            else
            {
                CleanupCrashMarker();
                return null;
            }

            CleanupCrashMarker();
            return content;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to read crash marker");
            CleanupCrashMarker();
            return null;
        }
    }

    private static void CleanupCrashMarker()
    {
        try { File.Delete(CrashMarkerPath); } catch { }
        try { File.Delete(CrashMarkerSigPath); } catch { }
    }

    /// <summary>
    /// Computes HMAC-SHA256 of content using the settings encryption key.
    /// This ties crash marker authenticity to the same key used for settings,
    /// so only this app instance (with access to .settings.key) can create valid markers.
    /// </summary>
    private static string ComputeHmac(string content)
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BatteryNotifier");
        var keyPath = Path.Combine(settingsDir, ".settings.key");

        byte[] key;
        if (File.Exists(keyPath))
        {
            key = File.ReadAllBytes(keyPath);
        }
        else
        {
            // Fallback — use a deterministic key from machine-specific data
            key = SHA256.HashData(Encoding.UTF8.GetBytes(
                Environment.MachineName + Environment.UserName + "BatteryNotifier"));
        }

        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hmac = HMACSHA256.HashData(key, contentBytes);
        return Convert.ToHexStringLower(hmac);
    }

    // ── Log Collection ───────────────────────────────────────────

    /// <summary>
    /// Collects recent log entries, sanitized of PII and markdown, ready for sharing.
    /// </summary>
    public static string CollectSanitizedLogs()
    {
        var sb = new StringBuilder();
        var logDir = BatteryNotifierLoggerConfig.GetLogDirectory();

        sb.AppendLine("## System Info");
        sb.AppendLine($"- Version: {Constants.ApplicationVersion}");
        sb.AppendLine($"- OS: {SanitizeContent(RuntimeInformation.OSDescription)}");
        sb.AppendLine($"- Arch: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"- Runtime: {SanitizeContent(RuntimeInformation.FrameworkDescription)}");
        sb.AppendLine();

        AppendLogFile(sb, logDir, "errors-", "## Error Log (recent)");
        AppendLogFile(sb, logDir, "app-", "## App Log (recent)");

        return sb.ToString();
    }

    private static void AppendLogFile(StringBuilder sb, string logDir, string prefix, string header)
    {
        try
        {
            if (!Directory.Exists(logDir)) return;

            // Find the most recent log file matching the prefix.
            // Validate filename format to prevent directory traversal via crafted filenames.
            var logFile = Directory.GetFiles(logDir, $"{prefix}*.log")
                .Where(f => Regex.IsMatch(Path.GetFileName(f), @"^(app|errors)-\d{8}\.log$"))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (logFile == null || !File.Exists(logFile)) return;

            var fileInfo = new FileInfo(logFile);
            if (fileInfo.Length > MaxLogFileSize)
            {
                sb.AppendLine(header);
                sb.AppendLine($"_(truncated — file is {fileInfo.Length / 1024} KB, showing last {MaxLogLines} lines)_");
                sb.AppendLine("```");
                var lines = ReadTailLines(logFile, MaxLogLines);
                foreach (var line in lines)
                    sb.AppendLine(SanitizeLine(line));
                sb.AppendLine("```");
            }
            else
            {
                var lines = File.ReadAllLines(logFile);
                var recentLines = lines.TakeLast(MaxLogLines).ToArray();

                sb.AppendLine(header);
                if (lines.Length > MaxLogLines)
                    sb.AppendLine($"_(showing last {MaxLogLines} of {lines.Length} lines)_");
                sb.AppendLine("```");
                foreach (var line in recentLines)
                    sb.AppendLine(SanitizeLine(line));
                sb.AppendLine("```");
            }
            sb.AppendLine();
        }
        catch (Exception ex)
        {
            sb.AppendLine($"{header}: _(failed to read: {SanitizeContent(ex.Message)})_");
        }
    }

    private static string[] ReadTailLines(string filePath, int lineCount)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var allLines = new List<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
                allLines.Add(line);
            return allLines.TakeLast(lineCount).ToArray();
        }
        catch
        {
            return [];
        }
    }

    // ── Content Sanitization ─────────────────────────────────────

    /// <summary>
    /// Strips PII from a log line AND neutralizes markdown/HTML injection.
    /// Log entries may contain user-controlled data (file paths from settings,
    /// notification messages) that could embed malicious markdown links,
    /// images, or HTML in the GitHub issue body.
    /// </summary>
    private static string SanitizeLine(string line)
    {
        // Truncate long lines (prevents single-line content bombs)
        if (line.Length > MaxLineLength)
            line = line[..MaxLineLength] + "…";

        // PII removal
        line = Regex.Replace(line, @"[A-Z]:\\Users\\[^\\]+", @"<drive>:\Users\<user>", RegexOptions.IgnoreCase);
        line = Regex.Replace(line, @"/(?:home|Users)/[^/\s]+", "/<home>/<user>");
        line = Regex.Replace(line, @"MachineName"":\s*""[^""]+""", @"MachineName"": ""<redacted>""");
        line = Regex.Replace(line, @"AppId"":\s*""[0-9a-f\-]{36}""", @"AppId"": ""<redacted>""", RegexOptions.IgnoreCase);

        // Markdown/HTML injection prevention
        // Inside ``` code fences most markdown is neutralized, but defense-in-depth:
        line = NeutralizeMarkdown(line);

        return line;
    }

    /// <summary>
    /// Neutralizes markdown and HTML constructs that could render as
    /// clickable links, images, or formatted content in a GitHub issue.
    /// </summary>
    private static string NeutralizeMarkdown(string text)
    {
        // Remove HTML tags entirely
        text = Regex.Replace(text, @"<[^>]+>", "");

        // Neutralize markdown links: [text](url) → [text](url)
        // Replace the outer brackets to break the link syntax
        text = Regex.Replace(text, @"\[([^\]]*)\]\(([^)]*)\)", "⟦$1⟧($2)");

        // Neutralize markdown images: ![alt](url)
        text = Regex.Replace(text, @"!\[([^\]]*)\]\(([^)]*)\)", "⟦img:$1⟧($2)");

        // Neutralize bare URLs that GitHub auto-links (outside of code fences)
        // We only do this for http/https — file:// and other schemes are already in code blocks
        text = Regex.Replace(text, @"https?://\S+", match =>
        {
            var url = match.Value;
            // Allow our own repo URL through
            if (url.StartsWith(Constants.SourceRepositoryUrl, StringComparison.OrdinalIgnoreCase))
                return url;
            // Break auto-linking by inserting a zero-width space after the protocol
            return url.Replace("://", ":/\u200B/");
        });

        return text;
    }

    /// <summary>
    /// Sanitizes a single content value (OS description, error message, etc.)
    /// for safe inclusion in the report.
    /// </summary>
    private static string SanitizeContent(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length > MaxLineLength)
            value = value[..MaxLineLength] + "…";
        return NeutralizeMarkdown(value);
    }

    /// <summary>
    /// Sanitizes an exception's stack trace and message.
    /// </summary>
    private static string SanitizeException(Exception exception)
    {
        var sb = new StringBuilder();
        var current = exception;
        int depth = 0;

        while (current != null && depth < 5)
        {
            if (depth > 0) sb.AppendLine("--- Inner Exception ---");
            sb.AppendLine($"Type: {current.GetType().FullName}");
            sb.AppendLine($"Message: {SanitizeLine(current.Message)}");
            if (current.StackTrace != null)
            {
                sb.AppendLine("StackTrace:");
                foreach (var frame in current.StackTrace.Split('\n'))
                    sb.AppendLine(SanitizeLine(frame.TrimEnd()));
            }
            current = current.InnerException;
            depth++;
        }

        return sb.ToString();
    }

    // ── Report Generation ────────────────────────────────────────

    /// <summary>
    /// Builds a crash report combining crash details and recent logs.
    /// </summary>
    public static string BuildCrashReport(string crashDetails)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Crash Report");
        sb.AppendLine();
        sb.AppendLine("```");
        // Re-sanitize the crash details even though they were sanitized at write time,
        // because the marker file could have been modified between write and read.
        foreach (var line in crashDetails.Split('\n'))
            sb.AppendLine(SanitizeLine(line.TrimEnd()));
        sb.AppendLine("```");
        sb.AppendLine();
        sb.Append(CollectSanitizedLogs());
        return sb.ToString();
    }

    /// <summary>
    /// Builds a manual log report (user-initiated, no crash).
    /// </summary>
    public static string BuildManualReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Manual Log Report");
        sb.AppendLine();
        sb.AppendLine("_User-initiated log submission (no crash detected)._");
        sb.AppendLine();
        sb.Append(CollectSanitizedLogs());
        return sb.ToString();
    }

    // ── Delivery ─────────────────────────────────────────────────

    /// <summary>
    /// Opens a pre-filled GitHub issue in the default browser.
    /// Enforces rate limiting — returns false if cooldown hasn't elapsed.
    /// The user reviews the content and submits manually.
    /// </summary>
    public static bool OpenGitHubIssue(string title, string body)
    {
        if (!CanSendReport())
            return false;

        try
        {
            // Sanitize the title — no markdown, no newlines
            title = Regex.Replace(title, @"[\r\n]+", " ");
            if (title.Length > 100)
                title = title[..100];
            title = NeutralizeMarkdown(title);

            var encodedTitle = Uri.EscapeDataString(title);

            // GitHub has a ~8000 char URL limit. Truncate body if needed.
            const int maxBodyLength = 6000;
            if (body.Length > maxBodyLength)
            {
                body = body[..maxBodyLength] + "\n\n_(truncated — full log available locally)_";
            }

            var encodedBody = Uri.EscapeDataString(body);
            var url = $"{IssueRepoUrl}/issues/new?title={encodedTitle}&body={encodedBody}&labels=bug,crash-report";

            OpenUrl(url);
            RecordReportSent();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open GitHub issue URL");
            return false;
        }
    }

    /// <summary>
    /// Saves the report to a file and returns the path.
    /// Not rate-limited (local file only, no external delivery).
    /// </summary>
    public static string SaveReportToFile(string report)
    {
        try
        {
            var reportsDir = Path.Combine(DataDirectory, "Reports");
            Directory.CreateDirectory(reportsDir);

            var fileName = $"report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md";
            var filePath = Path.Combine(reportsDir, fileName);

            File.WriteAllText(filePath, report);

            // Clean up old reports (keep last 10)
            var oldReports = Directory.GetFiles(reportsDir, "report-*.md")
                .OrderByDescending(f => f)
                .Skip(10);
            foreach (var old in oldReports)
            {
                try { File.Delete(old); } catch { }
            }

            return filePath;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save report to file");
            return string.Empty;
        }
    }

    private static void OpenUrl(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var psi = new ProcessStartInfo("open") { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var p = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            var psi = new ProcessStartInfo("xdg-open") { UseShellExecute = false };
            psi.ArgumentList.Add(url);
            using var p = Process.Start(psi);
        }
    }
}
