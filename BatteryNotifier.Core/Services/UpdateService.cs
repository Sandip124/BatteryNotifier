using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Checks for updates via GitHub Releases API and notifies the app.
/// Opens the GitHub release page in the browser for manual download.
/// </summary>
public sealed class UpdateService : IDisposable
{
    private static readonly Lazy<UpdateService> _instance = new(() => new UpdateService());
    public static UpdateService Instance => _instance.Value;

    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);
    private readonly object _lock = new();
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary>Tracks the last version we notified about to avoid repeat toasts.</summary>
    private string? _lastNotifiedVersion;

    /// <summary>True while a manual check is in progress (prevents re-entrancy).</summary>
    private int _isChecking;

    public event EventHandler<UpdateAvailableEventArgs>? UpdateAvailable;

    /// <summary>Only URLs matching this origin are trusted for browser open.</summary>
    private const string TrustedUrlPrefix = "https://github.com/Sandip124/BatteryNotifier";

    /// <summary>Max response size to prevent OOM from a malicious API response.</summary>
    private const long MaxResponseBytes = 1024 * 1024; // 1 MB

    private UpdateService()
    {
        _logger = BatteryNotifierAppLogger.ForContext<UpdateService>();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"BatteryNotifier/{Constants.ApplicationVersion}");
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _httpClient.MaxResponseContentBufferSize = MaxResponseBytes;
    }

    /// <summary>
    /// Starts periodic background update checks. Thread-safe — second call is a no-op.
    /// </summary>
    public void StartBackgroundChecks()
    {
        lock (_lock)
        {
            if (_cts != null) return;
            _cts = new CancellationTokenSource();
        }

        _ = RunPeriodicCheckAsync(_cts.Token);
    }

    /// <summary>
    /// Performs a single update check. Returns release info if an update is available.
    /// </summary>
    public async Task<GitHubRelease?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        try
        {
            var apiUrl = "https://api.github.com/repos/Sandip124/BatteryNotifier/releases/latest";
            var response = await _httpClient.GetAsync(apiUrl, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Debug("GitHub API returned {StatusCode}", response.StatusCode);
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync(
                GitHubReleaseContext.Default.GitHubRelease, ct).ConfigureAwait(false);

            if (release == null || string.IsNullOrEmpty(release.TagName))
                return null;

            // Validate the release URL points to our trusted GitHub repo
            // to prevent open redirect via compromised API response
            if (!release.HtmlUrl.StartsWith(TrustedUrlPrefix, StringComparison.OrdinalIgnoreCase))
            {
                _logger.Warning("Rejected release with untrusted URL: {Url}",
                    release.HtmlUrl.Length > 100 ? release.HtmlUrl[..100] : release.HtmlUrl);
                return null;
            }

            var remoteVersion = ParseVersion(release.TagName);
            var currentVersion = ParseVersion(Constants.ApplicationVersion);

            if (remoteVersion != null && currentVersion != null && remoteVersion > currentVersion)
            {
                _logger.Information("Update available: {Current} → {Remote}",
                    currentVersion, remoteVersion);
                return release;
            }

            _logger.Debug("No update available (current: {Current}, latest: {Remote})",
                Constants.ApplicationVersion, release.TagName);
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Update check failed");
            return null;
        }
    }

    /// <summary>
    /// Manual check triggered by the user. Returns the result for UI feedback.
    /// Re-entrant safe — concurrent calls return null immediately.
    /// </summary>
    public async Task<UpdateCheckResult> CheckForUpdateManualAsync(CancellationToken ct = default)
    {
        // Guard against re-entrancy (user spam-clicking)
        if (Interlocked.CompareExchange(ref _isChecking, 1, 0) != 0)
            return new UpdateCheckResult(null, CheckStatus.AlreadyChecking);

        try
        {
            var release = await CheckForUpdateAsync(ct).ConfigureAwait(false);
            return new UpdateCheckResult(release,
                release != null ? CheckStatus.UpdateAvailable : CheckStatus.UpToDate);
        }
        catch (Exception)
        {
            return new UpdateCheckResult(null, CheckStatus.Failed);
        }
        finally
        {
            Interlocked.Exchange(ref _isChecking, 0);
        }
    }

    private async Task RunPeriodicCheckAsync(CancellationToken ct)
    {
        // Initial delay — don't check immediately on startup
        try { await Task.Delay(TimeSpan.FromMinutes(2), ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { return; }

        // Check once, then start periodic timer
        await CheckAndNotifyAsync(ct).ConfigureAwait(false);

        _timer = new PeriodicTimer(_checkInterval);
        try
        {
            while (await _timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                await CheckAndNotifyAsync(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private async Task CheckAndNotifyAsync(CancellationToken ct)
    {
        var release = await CheckForUpdateAsync(ct).ConfigureAwait(false);
        if (release == null) return;

        // Don't re-notify for the same version the user already saw
        var tag = release.TagName;
        lock (_lock)
        {
            if (tag == _lastNotifiedVersion) return;
            _lastNotifiedVersion = tag;
        }

        UpdateAvailable?.Invoke(this, new UpdateAvailableEventArgs(release));
    }

    private static Version? ParseVersion(string versionString)
    {
        // Strip leading 'v' or 'V'
        var cleaned = versionString.TrimStart('v', 'V').Trim();
        return Version.TryParse(cleaned, out var version) ? version : null;
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _timer?.Dispose();
        _httpClient.Dispose();
    }
}

public enum CheckStatus
{
    UpToDate,
    UpdateAvailable,
    Failed,
    AlreadyChecking
}

public class UpdateCheckResult
{
    public GitHubRelease? Release { get; }
    public CheckStatus Status { get; }
    public UpdateCheckResult(GitHubRelease? release, CheckStatus status)
    {
        Release = release;
        Status = status;
    }
}

public class UpdateAvailableEventArgs : EventArgs
{
    public GitHubRelease Release { get; }
    public UpdateAvailableEventArgs(GitHubRelease release) => Release = release;
}

public class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = "";

    [JsonPropertyName("published_at")]
    public DateTimeOffset? PublishedAt { get; set; }

    [JsonPropertyName("assets")]
    public GitHubAsset[] Assets { get; set; } = [];
}

public class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

[JsonSerializable(typeof(GitHubRelease))]
internal partial class GitHubReleaseContext : JsonSerializerContext;
