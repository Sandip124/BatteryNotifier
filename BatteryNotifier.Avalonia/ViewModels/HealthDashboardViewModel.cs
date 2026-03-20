using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading;
using Avalonia.Threading;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Store;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class HealthDashboardViewModel : ViewModelBase, IDisposable
{
    private bool _disposed;
    private DateTime _lastUpdated = DateTime.UtcNow;
    private Timer? _displayTimer;

    // Cached last-known-good values — shown when current data is unavailable
    private BatteryHealthInfo? _cachedHealth;

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    public HealthDashboardViewModel()
    {
        RefreshCommand = ReactiveCommand.Create(Refresh);

        BatteryHealthService.Instance.HealthUpdated += OnHealthUpdated;
        UpdateFromHealth(BatteryHealthService.Instance.LatestHealth);

        // Subscribe to history updates
        BatteryHistoryService.Instance.ChargeHistoryUpdated += OnChargeHistoryUpdated;
        BatteryHistoryService.Instance.WearHistoryUpdated += OnWearHistoryUpdated;
        RefreshHistoryData();

        // Subscribe to power usage updates
        PowerUsageService.Instance.ProcessesUpdated += OnProcessesUpdated;
        UpdateTopProcesses(PowerUsageService.Instance.LatestProcesses);

        _displayTimer = new Timer(_ =>
        {
            Dispatcher.UIThread.Post(() => this.RaisePropertyChanged(nameof(LastUpdatedDisplay)));
        }, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));

    }

    private void OnHealthUpdated(object? sender, BatteryHealthInfo info)
    {
        Dispatcher.UIThread.Post(() => UpdateFromHealth(info));
    }

    private void Refresh()
    {
        var info = BatteryHealthService.Instance.Refresh();
        UpdateFromHealth(info);
    }

    private void UpdateFromHealth(BatteryHealthInfo? info)
    {
        if (info == null && _cachedHealth == null)
        {
            SetLoadingState();
            return;
        }

        var store = Core.Store.BatteryManagerStore.Instance;

        // No battery present (desktop) or battery not detected
        if (store.HasNoBattery || store.IsUnknown)
        {
            SetNoBatteryState();
            return;
        }

        var cached = MergeWithCache(info);
        _lastUpdated = DateTime.UtcNow;

        // Battery is fully degraded — all metrics empty/zero after fetch
        if (IsBatteryDataEmpty(cached))
        {
            SetDegradedBatteryState();
            return;
        }

        UpdateDisplayValues(cached);
        UpdateStatusValues(cached);

        this.RaisePropertyChanged(nameof(HealthColor));
        this.RaisePropertyChanged(nameof(TemperatureColor));
        this.RaisePropertyChanged(nameof(TemperatureStatusText));
        this.RaisePropertyChanged(nameof(LastUpdatedDisplay));
        this.RaisePropertyChanged(nameof(HasTopProcesses));
    }

    private void SetLoadingState()
    {
        HealthPercent = -1;
        CycleCountDisplay = "...";
        TemperatureDisplay = "...";
        VoltageDisplay = "...";
        PowerRateDisplay = "...";
        CurrentDisplay = "...";
        CapacityDisplay = "...";
        RecommendationMessage = "Fetching battery health data...";
    }

    private void SetNoBatteryState()
    {
        HealthPercent = -1;
        CycleCountDisplay = "N/A";
        TemperatureDisplay = "N/A";
        VoltageDisplay = "N/A";
        PowerRateDisplay = "N/A";
        CurrentDisplay = "N/A";
        CapacityDisplay = "N/A";
        IsCharging = false;
        ChargingStatusDisplay = "AC Power";
        HealthStatus = MetricStatus.Unavailable;
        RecommendationMessage = "No battery detected. This device is running on AC power only.";
    }

    private void SetDegradedBatteryState()
    {
        HealthPercent = 0;
        CycleCountDisplay = "N/A";
        TemperatureDisplay = "N/A";
        VoltageDisplay = "N/A";
        PowerRateDisplay = "N/A";
        CurrentDisplay = "N/A";
        CapacityDisplay = "0%";
        IsCharging = Core.Store.BatteryManagerStore.Instance.IsPluggedIn;
        ChargingStatusDisplay = "AC Power (battery degraded)";
        HealthStatus = MetricStatus.Poor;
        RecommendationMessage = "Battery appears fully degraded and cannot hold a charge. "
            + "The device is running on AC power only. Consider replacing the battery.";
    }

    private static bool IsBatteryDataEmpty(BatteryHealthInfo info)
    {
        return !info.HealthPercent.HasValue
            && !info.CycleCount.HasValue
            && !info.TemperatureCelsius.HasValue
            && !info.VoltageVolts.HasValue
            && !info.PowerRateWatts.HasValue;
    }

    private BatteryHealthInfo MergeWithCache(BatteryHealthInfo? info)
    {
        var fresh = info ?? new BatteryHealthInfo();
        var cached = _cachedHealth;

        if (cached == null)
        {
            _cachedHealth = fresh;
            return fresh;
        }

        if (info != null)
        {
            if (info.HealthPercent.HasValue) cached.HealthPercent = info.HealthPercent;
            if (info.CycleCount.HasValue) cached.CycleCount = info.CycleCount;
            if (info.DesignCycleCount.HasValue) cached.DesignCycleCount = info.DesignCycleCount;
            if (info.TemperatureCelsius.HasValue) cached.TemperatureCelsius = info.TemperatureCelsius;
            if (info.VoltageVolts.HasValue) cached.VoltageVolts = info.VoltageVolts;
            if (info.PowerRateWatts.HasValue) cached.PowerRateWatts = info.PowerRateWatts;
        }

        return cached;
    }

    private void UpdateDisplayValues(BatteryHealthInfo cached)
    {
        HealthPercent = cached.HealthPercent ?? -1;
        CycleCountDisplay = FormatCycleCount(cached);
        TemperatureDisplay = cached.TemperatureCelsius.HasValue ? $"{cached.TemperatureCelsius:F1}°C" : "--";
        VoltageDisplay = cached.VoltageVolts.HasValue ? $"{cached.VoltageVolts:F2} V" : "--";
        PowerRateDisplay = cached.PowerRateWatts.HasValue ? $"{cached.PowerRateWatts:F1} W" : "--";
        CurrentDisplay = cached is { VoltageVolts: > 0, PowerRateWatts: not null }
            ? $"{cached.PowerRateWatts.Value / cached.VoltageVolts.Value * 1000:F0} mA" : "--";
        CapacityDisplay = cached.HealthPercent.HasValue ? $"{cached.HealthPercent:F1}%" : "--";
        var store = Core.Store.BatteryManagerStore.Instance;
        IsCharging = store.IsPluggedIn;
        ChargingStatusDisplay = IsCharging ? "Charging" : "Discharging";
        ChargeTimeEstimate = ComputeChargeTimeEstimate(store, cached);
        RecommendationMessage = cached.RecommendationMessage;
    }

    private void UpdateStatusValues(BatteryHealthInfo cached)
    {
        CycleStatus = cached.CycleStatus;
        TemperatureStatus = cached.TemperatureStatus;
        HealthStatus = cached.HealthStatus;
    }

    private static string FormatCycleCount(BatteryHealthInfo cached)
    {
        if (!cached.CycleCount.HasValue) return "--";
        return cached.DesignCycleCount.HasValue
            ? $"{cached.CycleCount} / {cached.DesignCycleCount}"
            : cached.CycleCount.ToString()!;
    }

    private static string ComputeChargeTimeEstimate(
        Core.Store.BatteryManagerStore store, BatteryHealthInfo health)
    {
        if (store.HasNoBattery || store.IsUnknown) return string.Empty;

        var result = FormatOsReportedTime(store) ?? EstimateTimeFromRate(store, health);
        if (result is not { } timeStr) return string.Empty;

        return store.IsCharging ? timeStr.charging : timeStr.discharging;
    }

    private static (string charging, string discharging)? FormatOsReportedTime(
        Core.Store.BatteryManagerStore store)
    {
        if (store.BatteryLifeRemaining <= 0) return null;

        var ts = store.BatteryLifeRemainingInSeconds;
        var h = (int)ts.TotalHours;
        var formatted = h > 0 ? $"{h}h {ts.Minutes}m" : $"{ts.Minutes}m";

        return ($"Full in ~{formatted}", $"~{formatted} remaining");
    }

    private static (string charging, string discharging)? EstimateTimeFromRate(
        Core.Store.BatteryManagerStore store, BatteryHealthInfo health)
    {
        if (health.PowerRateWatts is not > 0 || !health.HealthPercent.HasValue)
            return null;

        var pct = store.BatteryLifePercent;
        var remainPct = store.IsCharging ? (100 - pct) : pct;
        if (remainPct <= 0) return null;

        // Rough estimate: assume 50Wh typical battery, linear rate
        var estimatedHours = remainPct / 100.0 * (health.HealthPercent.Value / 100.0)
            * 50.0 / health.PowerRateWatts.Value;
        if (estimatedHours is < 0.01 or > 48) return null;

        var totalMin = (int)(estimatedHours * 60);
        var formatted = totalMin >= 60 ? $"{totalMin / 60}h {totalMin % 60}m" : $"{totalMin}m";

        return ($"~{formatted} to 80% (estimated)", $"~{formatted} remaining (estimated)");
    }

    public double HealthPercent
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = -1;

    public string CycleCountDisplay
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "...";

    public string TemperatureDisplay
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "...";

    public string VoltageDisplay
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "...";

    public string PowerRateDisplay
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "...";

    public string CurrentDisplay
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "...";

    public string CapacityDisplay
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "...";

    public bool IsCharging
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string ChargingStatusDisplay
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "...";

    public string ChargeTimeEstimate
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public string HealthColor => HealthStatus switch
    {
        MetricStatus.Good => "#388E3C",
        MetricStatus.Fair => "#F57A00",
        MetricStatus.Poor => "#D32F2F",
        _ => "#8A8A8A"
    };

    public string TemperatureColor => TemperatureStatus switch
    {
        MetricStatus.Good => "#0288D1",  // cool blue
        MetricStatus.Fair => "#F57A00",  // warm amber
        MetricStatus.Poor => "#D32F2F",  // hot red
        _ => "#8A8A8A"
    };

    public string TemperatureStatusText => TemperatureStatus switch
    {
        MetricStatus.Good => "Normal",
        MetricStatus.Fair => "Warm",
        MetricStatus.Poor => "Too Hot",
        _ => "Not supported"
    };


    public string RecommendationMessage
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public MetricStatus HealthStatus
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public MetricStatus CycleStatus
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public MetricStatus TemperatureStatus
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string LastUpdatedDisplay
    {
        get
        {
            var elapsed = DateTime.UtcNow - _lastUpdated;
            if (elapsed.TotalSeconds < 10) return "Just now";
            if (elapsed.TotalMinutes < 1) return $"{(int)elapsed.TotalSeconds}s ago";
            return $"{(int)elapsed.TotalMinutes}m ago";
        }
    }

    // ── Battery History ─────────────────────────────────────────

    public IReadOnlyList<ChargeHistoryEntry>? ChargeHistory
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public IReadOnlyList<WearHistoryEntry>? WearHistory
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string WearSummaryText
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public bool HasChargeHistory => ChargeHistory is { Count: >= 2 };
    public bool HasWearHistory => WearHistory is { Count: >= 2 };

    // ── Top Battery Drainers ────────────────────────────────────

    public IReadOnlyList<ProcessDisplayItem>? TopProcesses
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string DrainersSummary
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    /// <summary>Left border color for the summary — matches severity of the top drainer.</summary>
    public string DrainersAccentColor
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "#FFAB00";

    /// <summary>
    /// Card is only shown when: on battery, has process data, AND has real battery metrics
    /// (power draw or time remaining). Raw CPU% alone isn't useful in a battery app.
    /// </summary>
    public bool HasTopProcesses =>
        TopProcesses is { Count: >= 1 }
        && !BatteryManagerStore.Instance.IsPluggedIn
        && HasBatteryMetrics;
    public bool HasDrainersSummary => !string.IsNullOrEmpty(DrainersSummary);

    private static bool HasBatteryMetrics
    {
        get
        {
            var store = BatteryManagerStore.Instance;
            var power = BatteryHealthService.Instance.LatestHealth?.PowerRateWatts;
            return power is > 0 || (store.BatteryLifeRemaining > 0 && !store.IsPluggedIn);
        }
    }

    private void OnProcessesUpdated(object? sender, IReadOnlyList<ProcessPowerInfo> e)
    {
        Dispatcher.UIThread.Post(() => UpdateTopProcesses(e));
    }

    private void UpdateTopProcesses(IReadOnlyList<ProcessPowerInfo>? processes)
    {
        if (processes is { Count: > 0 } && HasBatteryMetrics)
            BuildDrainersDisplay(processes);
        else
            ClearDrainersDisplay();

        this.RaisePropertyChanged(nameof(HasTopProcesses));
        this.RaisePropertyChanged(nameof(HasDrainersSummary));
    }

    private void BuildDrainersDisplay(IReadOnlyList<ProcessPowerInfo> processes)
    {
        var systemPower = BatteryHealthService.Instance.LatestHealth?.PowerRateWatts;
        var store = BatteryManagerStore.Instance;

        TopProcesses = processes.Select(p => new ProcessDisplayItem
        {
            Name = p.Name,
            CpuPercent = p.CpuPercent,
            Pid = p.Pid,
            PowerDisplay = FormatBatteryImpact(p.CpuPercent, systemPower, store),
            Tip = ProcessTips.GetTip(p.Name),
        }).ToList();

        DrainersSummary = ComputeDrainersSummary(processes[0], systemPower, store);
        DrainersAccentColor = processes[0].CpuPercent switch
        {
            > 50 => "#FF1744",
            > 20 => "#FFAB00",
            _ => "#00E676"
        };
    }

    private void ClearDrainersDisplay()
    {
        TopProcesses = null;
        DrainersSummary = string.Empty;
    }

    private static string? FormatBatteryImpact(
        double cpuPercent, double? systemPower, BatteryManagerStore store)
    {
        return FormatTimeCost(cpuPercent, store)
            ?? (systemPower is > 0 ? $"~{systemPower.Value * cpuPercent / 100:F1}W" : null);
    }

    /// <summary>
    /// Formats the battery time cost of a process, e.g. "~25min" or "~1h 25m".
    /// Returns null if impact is negligible (&lt;1 min) or data unavailable.
    /// </summary>
    private static string? FormatTimeCost(double cpuPercent, BatteryManagerStore store)
    {
        if (store.BatteryLifeRemaining <= 0 || store.IsPluggedIn)
            return null;

        var fraction = Math.Min(cpuPercent / 100.0, 0.8);
        var costSeconds = store.BatteryLifeRemaining * fraction / (1 - fraction);
        var costMinutes = (int)(costSeconds / 60);

        if (costMinutes < 1) return null;

        // Round: >=10 min to nearest 5, otherwise show exact
        if (costMinutes >= 10)
            costMinutes = costMinutes / 5 * 5;

        return costMinutes >= 60
            ? $"~{costMinutes / 60}h {costMinutes % 60}m"
            : $"~{costMinutes}min";
    }

    private static string ComputeDrainersSummary(
        ProcessPowerInfo top, double? systemPower, BatteryManagerStore store)
    {
        var topTip = ProcessTips.GetTip(top.Name);
        var action = topTip != null ? $" {topTip}." : string.Empty;

        // Best: time-based — "Chrome is costing you ~25min of battery. Close unused tabs."
        var timeCost = FormatTimeCost(top.CpuPercent, store);
        if (timeCost != null)
        {
            return $"{top.Name} is costing you {timeCost} of battery life.{action}";
        }

        // Watts — "Chrome is draining ~6.3W from your battery. Close unused tabs."
        if (systemPower is > 0)
        {
            var watts = systemPower.Value * top.CpuPercent / 100;
            return $"{top.Name} is draining ~{watts:F1}W from your battery.{action}";
        }

        return string.Empty;
    }

    private void OnChargeHistoryUpdated()
    {
        Dispatcher.UIThread.Post(RefreshChargeHistory);
    }

    private void OnWearHistoryUpdated()
    {
        Dispatcher.UIThread.Post(RefreshWearHistory);
    }

    private void RefreshHistoryData()
    {
        RefreshChargeHistory();
        RefreshWearHistory();
    }

    private void RefreshChargeHistory()
    {
        ChargeHistory = BatteryHistoryService.Instance.GetChargeHistory();
        this.RaisePropertyChanged(nameof(HasChargeHistory));
    }

    private void RefreshWearHistory()
    {
        WearHistory = BatteryHistoryService.Instance.GetWearHistory();
        WearSummaryText = BatteryHistoryService.Instance.GetWearSummary() ?? string.Empty;
        this.RaisePropertyChanged(nameof(HasWearHistory));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _displayTimer?.Dispose();
        _displayTimer = null;
        BatteryHealthService.Instance.HealthUpdated -= OnHealthUpdated;
        BatteryHistoryService.Instance.ChargeHistoryUpdated -= OnChargeHistoryUpdated;
        BatteryHistoryService.Instance.WearHistoryUpdated -= OnWearHistoryUpdated;
        PowerUsageService.Instance.ProcessesUpdated -= OnProcessesUpdated;
    }
}
