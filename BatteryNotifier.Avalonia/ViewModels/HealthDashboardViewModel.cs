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
    private DateTime _lastUpdated = DateTime.Now;
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
        try
        {
            var info = BatteryHealthService.Instance.Refresh();
            UpdateFromHealth(info);
        }
        catch { }
    }

    private void UpdateFromHealth(BatteryHealthInfo? info)
    {
        if (info == null && _cachedHealth == null)
        {
            HealthPercent = -1;
            CycleCountDisplay = "...";
            TemperatureDisplay = "...";
            VoltageDisplay = "...";
            PowerRateDisplay = "...";
            CurrentDisplay = "...";
            CapacityDisplay = "...";
            RecommendationMessage = "Fetching battery health data...";
            return;
        }

        // Merge: use fresh data where available, fall back to cached
        var fresh = info ?? new BatteryHealthInfo();
        var cached = _cachedHealth;

        // Update cache — only overwrite fields that have actual values
        if (cached == null)
        {
            _cachedHealth = fresh;
            cached = fresh;
        }
        else if (info != null)
        {
            if (info.HealthPercent.HasValue) cached.HealthPercent = info.HealthPercent;
            if (info.CycleCount.HasValue) cached.CycleCount = info.CycleCount;
            if (info.DesignCycleCount.HasValue) cached.DesignCycleCount = info.DesignCycleCount;
            if (info.TemperatureCelsius.HasValue) cached.TemperatureCelsius = info.TemperatureCelsius;
            if (info.VoltageVolts.HasValue) cached.VoltageVolts = info.VoltageVolts;
            if (info.PowerRateWatts.HasValue) cached.PowerRateWatts = info.PowerRateWatts;
        }

        _lastUpdated = DateTime.Now;

        // Use cached (merged) values for display
        HealthPercent = cached.HealthPercent ?? -1;
        CycleCountDisplay = cached.CycleCount.HasValue
            ? cached.DesignCycleCount.HasValue
                ? $"{cached.CycleCount} / {cached.DesignCycleCount}"
                : cached.CycleCount.ToString()!
            : "--";
        TemperatureDisplay = cached.TemperatureCelsius.HasValue ? $"{cached.TemperatureCelsius:F1}°C" : "--";
        VoltageDisplay = cached.VoltageVolts.HasValue ? $"{cached.VoltageVolts:F2} V" : "--";
        PowerRateDisplay = cached.PowerRateWatts.HasValue ? $"{cached.PowerRateWatts:F1} W" : "--";
        CurrentDisplay = cached.VoltageVolts.HasValue && cached.PowerRateWatts.HasValue && cached.VoltageVolts.Value > 0
            ? $"{cached.PowerRateWatts.Value / cached.VoltageVolts.Value * 1000:F0} mA" : "--";
        CapacityDisplay = cached.HealthPercent.HasValue ? $"{cached.HealthPercent:F1}%" : "--";
        IsCharging = Core.Store.BatteryManagerStore.Instance.IsPluggedIn;
        ChargingStatusDisplay = IsCharging ? "Charging" : "Discharging";
        RecommendationMessage = cached.RecommendationMessage;

        CycleStatus = cached.CycleStatus;
        TemperatureStatus = cached.TemperatureStatus;
        HealthStatus = cached.HealthStatus;

        this.RaisePropertyChanged(nameof(HealthColor));
        this.RaisePropertyChanged(nameof(TemperatureColor));
        this.RaisePropertyChanged(nameof(TemperatureStatusText));
        this.RaisePropertyChanged(nameof(LastUpdatedDisplay));
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

    public string PowerColor => "#0288D1";


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
            var elapsed = DateTime.Now - _lastUpdated;
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
