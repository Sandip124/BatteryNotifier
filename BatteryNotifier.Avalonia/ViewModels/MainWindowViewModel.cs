using System;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Store;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly AppSettings _settings = AppSettings.Instance;

    private double _batteryPercentage;
    private bool _isCharging;
    private string _batteryStatus = string.Empty;
    private Bitmap? _batteryImage;
    private string _timeRemaining = string.Empty;
    private bool _fullBatteryNotification;
    private bool _lowBatteryNotification;
    private bool _isTopmost;
    private SettingsViewModel? _currentView;
    private bool _disposed;

    public MainWindowViewModel()
    {
        _isTopmost = _settings.PinToWindow;
        _fullBatteryNotification = _settings.FullBatteryNotification;
        _lowBatteryNotification = _settings.LowBatteryNotification;

        NavigateToSettingsCommand = ReactiveCommand.Create(NavigateToSettings);

        RefreshBatteryStatus();

        try
        {
            BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged += OnPowerLineStatusChanged;
        }
        catch { /* Battery monitoring not available on this platform */ }

        this.WhenAnyValue(x => x.FullBatteryNotification)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.FullBatteryNotification = enabled;
                _settings.Save();
            });

        this.WhenAnyValue(x => x.LowBatteryNotification)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.LowBatteryNotification = enabled;
                _settings.Save();
            });
    }

    private void OnBatteryStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshBatteryStatus);
    }

    private void OnPowerLineStatusChanged(object? sender, BatteryStatusEventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshBatteryStatus);
    }

    private void RefreshBatteryStatus()
    {
        var store = BatteryManagerStore.Instance;

        BatteryPercentage = store.BatteryLifePercent;
        IsCharging = store.IsCharging;

        if (store.HasNoBattery)
            BatteryStatus = "No Battery";
        else if (store.IsUnknown)
            BatteryStatus = "Unknown";
        else if (store.IsCharging)
            BatteryStatus = "Charging";
        else
            BatteryStatus = "Discharging";

        if (store.BatteryLifeRemaining > 0 && !store.IsCharging)
        {
            var ts = store.BatteryLifeRemainingInSeconds;
            TimeRemaining = $"{(int)ts.TotalHours}h {ts.Minutes}m Remaining";
        }
        else
        {
            TimeRemaining = store.IsCharging ? "Charging..." : string.Empty;
        }

        var assetName = store.BatteryState switch
        {
            BatteryState.Full => "FullBattery.png",
            BatteryState.Adequate => "FullBattery.png",
            BatteryState.Sufficient => "Sufficient.png",
            BatteryState.Low => "LowBattery.png",
            BatteryState.Critical => "LowBattery.png",
            _ => "Sufficient.png"
        };

        BatteryImage = LoadAsset(assetName);
    }

    private static Bitmap? LoadAsset(string fileName)
    {
        try
        {
            var uri = new Uri($"avares://BatteryNotifier.Avalonia/Assets/{fileName}");
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    public double BatteryPercentage
    {
        get => _batteryPercentage;
        set => this.RaiseAndSetIfChanged(ref _batteryPercentage, value);
    }

    public bool IsCharging
    {
        get => _isCharging;
        set => this.RaiseAndSetIfChanged(ref _isCharging, value);
    }

    public string BatteryStatus
    {
        get => _batteryStatus;
        set => this.RaiseAndSetIfChanged(ref _batteryStatus, value);
    }

    public Bitmap? BatteryImage
    {
        get => _batteryImage;
        set => this.RaiseAndSetIfChanged(ref _batteryImage, value);
    }

    public string TimeRemaining
    {
        get => _timeRemaining;
        set => this.RaiseAndSetIfChanged(ref _timeRemaining, value);
    }

    public bool FullBatteryNotification
    {
        get => _fullBatteryNotification;
        set => this.RaiseAndSetIfChanged(ref _fullBatteryNotification, value);
    }

    public bool LowBatteryNotification
    {
        get => _lowBatteryNotification;
        set => this.RaiseAndSetIfChanged(ref _lowBatteryNotification, value);
    }

    public bool IsTopmost
    {
        get => _isTopmost;
        set => this.RaiseAndSetIfChanged(ref _isTopmost, value);
    }

    public SettingsViewModel? CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public ReactiveCommand<Unit, Unit> NavigateToSettingsCommand { get; }

    private void NavigateToSettings()
    {
        var settingsVm = new SettingsViewModel(NavigateToMain);
        settingsVm.PinToWindowChanged += (_, pinned) => IsTopmost = pinned;
        CurrentView = settingsVm;
    }

    private void NavigateToMain()
    {
        CurrentView = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        try
        {
            BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged -= OnPowerLineStatusChanged;
        }
        catch { }
        _disposed = true;
    }
}
