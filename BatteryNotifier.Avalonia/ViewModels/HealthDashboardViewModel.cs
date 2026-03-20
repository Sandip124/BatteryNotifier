using System;
using System.Reactive;
using System.Threading;
using Avalonia.Threading;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;
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
        MetricStatus.Good => "#388E3C",
        MetricStatus.Fair => "#F57A00",
        MetricStatus.Poor => "#D32F2F",
        _ => "#8A8A8A"
    };

    public string TemperatureStatusText => TemperatureStatus switch
    {
        MetricStatus.Good => "Normal",
        MetricStatus.Fair => "Warm",
        MetricStatus.Poor => "Too Hot",
        _ => "--"
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _displayTimer?.Dispose();
        _displayTimer = null;
        BatteryHealthService.Instance.HealthUpdated -= OnHealthUpdated;
    }
}
