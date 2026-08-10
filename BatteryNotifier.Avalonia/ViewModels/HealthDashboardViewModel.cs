using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using Avalonia.Threading;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Utils;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class HealthDashboardViewModel : ViewModelBase, IDisposable
{
    private bool _disposed;
    private DateTime _lastUpdated = DateTime.UtcNow;
    private Timer? _displayTimer;

    private BatteryHealthInfo? _cachedHealth;

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    public HealthDashboardViewModel()
    {
        RefreshCommand = ReactiveCommand.Create(Refresh);

        BatteryHealthService.Instance.HealthUpdated += OnHealthUpdated;
        UpdateFromHealth(BatteryHealthService.Instance.LatestHealth);

        BatteryHistoryService.Instance.ChargeHistoryUpdated += OnChargeHistoryUpdated;
        BatteryHistoryService.Instance.WearHistoryUpdated += OnWearHistoryUpdated;
        RefreshHistoryData();

        _displayTimer = new Timer(_ =>
        {
            if (_disposed) return;
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
        HealthStatus = MetricStatus.Unavailable;
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
        HealthStatus = MetricStatus.Poor;
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
        if (_cachedHealth is not { } cached)
            return _cachedHealth = info ?? new BatteryHealthInfo();

        if (info != null)
        {
            cached.HealthPercent = info.HealthPercent ?? cached.HealthPercent;
            cached.CycleCount = info.CycleCount ?? cached.CycleCount;
            cached.DesignCycleCount = info.DesignCycleCount ?? cached.DesignCycleCount;
            cached.TemperatureCelsius = info.TemperatureCelsius ?? cached.TemperatureCelsius;
            cached.VoltageVolts = info.VoltageVolts ?? cached.VoltageVolts;
            cached.PowerRateWatts = info.PowerRateWatts ?? cached.PowerRateWatts;
        }

        return cached;
    }

    private void UpdateDisplayValues(BatteryHealthInfo cached)
    {
        HealthPercent = cached.HealthPercent ?? -1;
        CycleCountDisplay = FormatCycleCount(cached);
        TemperatureDisplay = cached.TemperatureCelsius.HasValue ? $"{cached.TemperatureCelsius:F1}°C" : "N/A";
        VoltageDisplay = cached.VoltageVolts.HasValue ? $"{cached.VoltageVolts:F2} V" : "N/A";
        PowerRateDisplay = cached.PowerRateWatts.HasValue ? $"{cached.PowerRateWatts:F1} W" : "N/A";
        CurrentDisplay = cached is { VoltageVolts: > 0, PowerRateWatts: not null }
            ? $"{cached.PowerRateWatts.Value / cached.VoltageVolts.Value * 1000:F0} mA" : "N/A";
        CapacityDisplay = cached.HealthPercent.HasValue ? $"{cached.HealthPercent:F1}%" : "N/A";
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

    public string HealthColor => HealthColors.ForHealth(HealthStatus);
    public string TemperatureColor => HealthColors.ForTemperature(TemperatureStatus);
    public string TemperatureStatusText => HealthColors.TemperatureText(TemperatureStatus);

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

    public string LastUpdatedDisplay => TimeFormat.Ago(DateTime.UtcNow - _lastUpdated);

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
    }
}
