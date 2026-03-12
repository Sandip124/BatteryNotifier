using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BatteryNotifier.Core;
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
    private SettingsViewModel? _currentView;
    private bool _disposed;
    private IDisposable? _fullBatteryNotificationSub;
    private IDisposable? _lowBatteryNotificationSub;

    public MainWindowViewModel()
    {
        _fullBatteryNotification = _settings.FullBatteryNotification;
        _lowBatteryNotification = _settings.LowBatteryNotification;

        NavigateToSettingsCommand = ReactiveCommand.Create(NavigateToSettings);
        OpenGitHubCommand = ReactiveCommand.Create(OpenGitHub);
        ExitCommand = ReactiveCommand.Create(ExitApplication);

        // Access BatteryMonitorService FIRST — its constructor does a synchronous
        // initial check that populates BatteryManagerStore, so the store has real
        // values before RefreshBatteryStatus() reads them.
        try
        {
            BatteryMonitorService.Instance.BatteryStatusChanged += OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged += OnPowerLineStatusChanged;
        }
        catch { /* Battery monitoring not available on this platform */ }

        RefreshBatteryStatus();

        _fullBatteryNotificationSub = this.WhenAnyValue(x => x.FullBatteryNotification)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.FullBatteryNotification = enabled;
                _settings.Save();
            });

        _lowBatteryNotificationSub = this.WhenAnyValue(x => x.LowBatteryNotification)
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
        IsCharging = store.IsCharging || store.IsPluggedIn;

        if (store.HasNoBattery)
            BatteryStatus = "No Battery";
        else if (store.IsUnknown)
            BatteryStatus = "Unknown";
        else if (store.IsCharging)
            BatteryStatus = "Charging";
        else if (store.IsPluggedIn)
            BatteryStatus = "Plugged In";
        else
            BatteryStatus = "Discharging";

        if (store.BatteryLifeRemaining > 0 && !store.IsCharging && !store.IsPluggedIn)
        {
            var ts = store.BatteryLifeRemainingInSeconds;
            TimeRemaining = $"{(int)ts.TotalHours}h {ts.Minutes}m Remaining";
        }
        else if (store.IsCharging)
        {
            TimeRemaining = "Charging...";
        }
        else
        {
            TimeRemaining = string.Empty;
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

    public SettingsViewModel? CurrentView
    {
        get => _currentView;
        set => this.RaiseAndSetIfChanged(ref _currentView, value);
    }

    public ReactiveCommand<Unit, Unit> NavigateToSettingsCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenGitHubCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public string Version => Constants.ApplicationVersion;

    private static void OpenGitHub()
    {
        try
        {
            var url = Constants.SourceRepositoryUrl;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else
                Process.Start("xdg-open", url);
        }
        catch { }
    }

    private static void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void NavigateToSettings()
    {
        CurrentView = new SettingsViewModel(NavigateToMain);
    }

    private void NavigateToMain()
    {
        var old = CurrentView;
        CurrentView = null;
        old?.Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _fullBatteryNotificationSub?.Dispose();
        _lowBatteryNotificationSub?.Dispose();
        CurrentView?.Dispose();
        try
        {
            BatteryMonitorService.Instance.BatteryStatusChanged -= OnBatteryStatusChanged;
            BatteryMonitorService.Instance.PowerLineStatusChanged -= OnPowerLineStatusChanged;
        }
        catch { }
        _disposed = true;
    }
}
