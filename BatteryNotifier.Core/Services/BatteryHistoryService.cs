using System.Text.Json;
using System.Text.Json.Serialization;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Store;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Records battery charge history (24h sparkline) and wear history (13-month trend).
/// Persists to JSON files in the app data directory.
/// </summary>
public sealed class BatteryHistoryService : IDisposable
{
    private static readonly Lazy<BatteryHistoryService> _instance = new(() => new BatteryHistoryService());
    public static BatteryHistoryService Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("BatteryHistoryService");

    private readonly Lock _chargeLock = new();
    private readonly Lock _wearLock = new();

    private List<ChargeHistoryEntry> _chargeHistory = [];
    private List<WearHistoryEntry> _wearHistory = [];

    private DateTimeOffset _lastChargeRecording = DateTimeOffset.MinValue;
    private DateTimeOffset _lastWearRecording = DateTimeOffset.MinValue;
    private DateTimeOffset _lastChargeFlush = DateTimeOffset.MinValue;

    private bool _disposed;
    private bool _initialWearRecorded;
    private CancellationTokenSource? _cts;

    // Throttle intervals
    private static readonly TimeSpan ChargeRecordInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ChargeFlushInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan WearRecordInterval = TimeSpan.FromHours(1);

    // Pruning limits
    private static readonly TimeSpan ChargeRetention = TimeSpan.FromHours(25);
    private static readonly TimeSpan WearRetention = TimeSpan.FromDays(395); // ~13 months

    public event Action? ChargeHistoryUpdated;
    public event Action? WearHistoryUpdated;

    private static string ChargeHistoryPath => Path.Combine(Constants.AppDataDirectory, "charge_history.json");
    private static string WearHistoryPath => Path.Combine(Constants.AppDataDirectory, "wear_history.json");

    private BatteryHistoryService()
    {
        LoadFromDisk();

        // Record an initial reading immediately so the sparkline has data from startup.
        // Flush to disk so it persists even if the app is closed quickly.
        RecordChargeReading();
        FlushChargeToDisk();

        try
        {
            BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged += OnPowerLineStatusChanged;
        }
        catch
        {
            // Battery monitoring not available on this platform
        }

        BatteryHealthService.Instance.HealthUpdated += OnHealthUpdated;

        // Periodic recording every 1 minute — ensures data flows even when
        // BatteryStatusChanged doesn't fire (level unchanged between polls)
        _cts = new CancellationTokenSource();
        _ = RunPeriodicRecordingAsync(_cts.Token);
    }

    private async Task RunPeriodicRecordingAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(ChargeRecordInterval);
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            RecordChargeReading();
        }
    }

    private void OnBatteryStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        RecordChargeReading();
    }

    private void OnPowerLineStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        // Force-record on charger plug/unplug regardless of throttle
        RecordChargeReading(bypassThrottle: true);
    }

    private void OnHealthUpdated(object? sender, BatteryHealthInfo info)
    {
        RecordWearReading(info);
    }

    private void RecordChargeReading(bool bypassThrottle = false)
    {
        var now = DateTimeOffset.UtcNow;
        if (!bypassThrottle && now - _lastChargeRecording < ChargeRecordInterval)
            return;

        var store = BatteryManagerStore.Instance;
        if (store.HasNoBattery || store.IsUnknown)
            return;

        var pct = (int)store.BatteryLifePercent;
        if (pct is < 0 or > 100)
            return;

        var entry = new ChargeHistoryEntry(
            now.ToUnixTimeSeconds(),
            (byte)pct,
            store.IsCharging || store.IsPluggedIn);

        lock (_chargeLock)
        {
            _chargeHistory.Add(entry);
            _lastChargeRecording = now;
            PruneChargeHistory();
        }

        // Flush to disk periodically, or eagerly when we have few entries
        bool shouldFlush;
        lock (_chargeLock)
        {
            shouldFlush = _chargeHistory.Count <= 5 || now - _lastChargeFlush >= ChargeFlushInterval;
        }
        if (shouldFlush)
        {
            _lastChargeFlush = now;
            FlushChargeToDisk();
        }

        ChargeHistoryUpdated?.Invoke();
    }

    private void RecordWearReading(BatteryHealthInfo info)
    {
        if (!info.HealthPercent.HasValue)
            return;

        var now = DateTimeOffset.UtcNow;

        // Always record the first health update per app launch, then throttle to 1h
        if (_initialWearRecorded && now - _lastWearRecording < WearRecordInterval)
            return;
        _initialWearRecorded = true;

        var entry = new WearHistoryEntry(
            now.ToUnixTimeSeconds(),
            info.HealthPercent.Value,
            info.CycleCount);

        lock (_wearLock)
        {
            _wearHistory.Add(entry);
            _lastWearRecording = now;
            PruneWearHistory();
        }

        FlushWearToDisk();
        WearHistoryUpdated?.Invoke();
    }

    private void PruneChargeHistory()
    {
        var cutoff = DateTimeOffset.UtcNow.Add(-ChargeRetention).ToUnixTimeSeconds();
        _chargeHistory.RemoveAll(e => e.TimestampUnixSeconds < cutoff);
    }

    private void PruneWearHistory()
    {
        var cutoff = DateTimeOffset.UtcNow.Add(-WearRetention).ToUnixTimeSeconds();
        _wearHistory.RemoveAll(e => e.TimestampUnixSeconds < cutoff);
    }

    public IReadOnlyList<ChargeHistoryEntry> GetChargeHistory()
    {
        lock (_chargeLock)
        {
            return _chargeHistory.ToList();
        }
    }

    public IReadOnlyList<WearHistoryEntry> GetWearHistory()
    {
        lock (_wearLock)
        {
            return _wearHistory.ToList();
        }
    }

    /// <summary>
    /// Returns a human-readable summary of wear trend (e.g., "Lost 2.1% this month").
    /// </summary>
    public string? GetWearSummary()
    {
        lock (_wearLock)
        {
            if (_wearHistory.Count < 2)
                return null;

            var oldest = _wearHistory[0];
            var newest = _wearHistory[^1];
            var diff = oldest.HealthPercent - newest.HealthPercent;
            var days = (newest.TimestampUnixSeconds - oldest.TimestampUnixSeconds) / 86400.0;

            if (diff <= 0)
                return "Stable";

            if (days < 30)
                return $"\u2193 {diff:F1}% recently";

            if (days <= 60)
                return $"\u2193 {diff:F1}% this month";

            var months = days / 30.0;
            var perMonth = diff / months;
            return $"\u2193 {diff:F1}% over {months:F0}mo (~{perMonth:F1}%/mo)";
        }
    }

    // ── Persistence ─────────────────────────────────────────────

    private void LoadFromDisk()
    {
        _chargeHistory = LoadFile<List<ChargeHistoryEntry>>(ChargeHistoryPath) ?? [];
        _wearHistory = LoadFile<List<WearHistoryEntry>>(WearHistoryPath) ?? [];

        // Set last-recording timestamps from loaded data to avoid duplicates
        if (_chargeHistory.Count > 0)
            _lastChargeRecording = DateTimeOffset.FromUnixTimeSeconds(_chargeHistory[^1].TimestampUnixSeconds);
        if (_wearHistory.Count > 0)
            _lastWearRecording = DateTimeOffset.FromUnixTimeSeconds(_wearHistory[^1].TimestampUnixSeconds);

        _lastChargeFlush = DateTimeOffset.UtcNow;
    }

    private static T? LoadFile<T>(string path) where T : class
    {
        try
        {
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, BatteryHistoryJsonContext.Default.Options);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load history from {Path}, starting fresh", path);
            return null;
        }
    }

    private void FlushChargeToDisk()
    {
        List<ChargeHistoryEntry> snapshot;
        lock (_chargeLock)
        {
            snapshot = [.. _chargeHistory];
        }
        WriteFile(ChargeHistoryPath, snapshot);
    }

    private void FlushWearToDisk()
    {
        List<WearHistoryEntry> snapshot;
        lock (_wearLock)
        {
            snapshot = [.. _wearHistory];
        }
        WriteFile(WearHistoryPath, snapshot);
    }

    private static void WriteFile<T>(string path, T data)
    {
        try
        {
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(data, BatteryHistoryJsonContext.Default.Options);
            var tmpPath = path + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to write history to {Path}", path);
        }
    }

    /// <summary>
    /// Forces an immediate flush of charge history to disk.
    /// Called on app shutdown to avoid losing the last ~5min of data.
    /// </summary>
    public void Flush()
    {
        FlushChargeToDisk();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        try
        {
            BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged -= OnPowerLineStatusChanged;
        }
        catch { /* not available */ }

        BatteryHealthService.Instance.HealthUpdated -= OnHealthUpdated;
        FlushChargeToDisk();
    }
}

[JsonSerializable(typeof(List<ChargeHistoryEntry>))]
[JsonSerializable(typeof(List<WearHistoryEntry>))]
[JsonSourceGenerationOptions(WriteIndented = false)]
internal partial class BatteryHistoryJsonContext : JsonSerializerContext;
