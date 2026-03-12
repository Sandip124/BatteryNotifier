using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using BatteryNotifier.Core;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Store;
using ReactiveUI;
using Serilog;

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
    private string _statusMessage = string.Empty;
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
        CheckForUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdates);
        SendLogsCommand = ReactiveCommand.Create(SendLogs);
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
        StatusMessage = PickStatusMessage(
            BatteryManagerStore.Instance.BatteryState,
            BatteryManagerStore.Instance.IsCharging);
        _ = ClearStatusMessageAfterDelay();

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
            var uri = new Uri($"avares://BatteryNotifier/Assets/{fileName}");
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
    public ReactiveCommand<Unit, Unit> CheckForUpdatesCommand { get; }
    public ReactiveCommand<Unit, Unit> SendLogsCommand { get; }
    public ReactiveCommand<Unit, Unit> ExitCommand { get; }

    public string Version => Constants.ApplicationVersion;

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    private static readonly string[] GreetingsFull =
    [
        "Your battery is vibing at 100%.",
        "Fully juiced! Time to unplug.",
        "Battery's living its best life.",
        "All topped up. You're golden!",
        "Full tank energy right here."
    ];

    private static readonly string[] GreetingsAdequate =
    [
        "Battery's looking great today!",
        "Smooth sailing ahead.",
        "You've got plenty of juice.",
        "All systems go. Carry on!",
        "Battery says: 'I'm chilling.'"
    ];

    private static readonly string[] GreetingsSufficient =
    [
        "Still going strong!",
        "Halfway there, keep cruising.",
        "Battery's holding steady.",
        "Not bad, not bad at all.",
        "Doing just fine over here."
    ];

    private static readonly string[] GreetingsLow =
    [
        "Getting a bit thirsty...",
        "Maybe find a charger soon?",
        "Battery's sending SOS vibes.",
        "Running on fumes here!",
        "A charger would be nice right about now."
    ];

    private static readonly string[] GreetingsCritical =
    [
        "MAYDAY! Plug in, plug in!",
        "Battery's on life support.",
        "We're in the danger zone!",
        "This is not a drill. Charge me!",
        "Counting down... find power NOW."
    ];

    private static readonly string[] GreetingsCharging =
    [
        "Charging up! Sit tight.",
        "Nom nom nom... delicious electricity.",
        "Sipping on some sweet power.",
        "Refueling in progress...",
        "Getting stronger by the minute!"
    ];

    private async Task ClearStatusMessageAfterDelay()
    {
        await Task.Delay(5000);
        Dispatcher.UIThread.Post(() => StatusMessage = string.Empty);
    }

    private static string PickStatusMessage(BatteryState state, bool isCharging)
    {
        if (isCharging)
            return GreetingsCharging[Random.Shared.Next(GreetingsCharging.Length)];

        var pool = state switch
        {
            BatteryState.Full => GreetingsFull,
            BatteryState.Adequate => GreetingsAdequate,
            BatteryState.Sufficient => GreetingsSufficient,
            BatteryState.Low => GreetingsLow,
            BatteryState.Critical => GreetingsCritical,
            _ => GreetingsAdequate
        };

        return pool[Random.Shared.Next(pool.Length)];
    }

    private static void OpenGitHub()
    {
        OpenUrlInBrowser(Constants.SourceRepositoryUrl);
    }

    private static void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private async Task CheckForUpdates()
    {
        var result = await UpdateService.Instance.CheckForUpdateManualAsync();

        switch (result.Status)
        {
            case CheckStatus.UpdateAvailable when result.Release != null:
                OpenUrlInBrowser(result.Release.HtmlUrl);
                break;

            case CheckStatus.UpToDate:
                Services.NotificationPlatformService.ShowNativeNotification(
                    "No Updates",
                    $"You're running the latest version ({Constants.ApplicationVersion}).");
                break;

            case CheckStatus.Failed:
                Services.NotificationPlatformService.ShowNativeNotification(
                    "Update Check Failed",
                    "Could not reach GitHub. Check your internet connection.");
                break;
        }
    }

    private static void SendLogs()
    {
        try
        {
            if (!CrashReporter.CanSendReport())
            {
                var remaining = CrashReporter.GetCooldownRemaining();
                Services.NotificationPlatformService.ShowNativeNotification(
                    "Rate Limited",
                    $"Please wait {remaining.TotalMinutes:F0} minutes before sending another report.");
                // Still save to file
                var report = CrashReporter.BuildManualReport();
                CrashReporter.SaveReportToFile(report);
                return;
            }

            var manualReport = CrashReporter.BuildManualReport();
            CrashReporter.SaveReportToFile(manualReport);
            CrashReporter.OpenGitHubIssue(
                $"[Log Report] v{Constants.ApplicationVersion}",
                manualReport);
        }
        catch (Exception ex)
        {
            BatteryNotifierAppLogger.Error(ex, "Failed to send logs from menu");
        }
    }

    private static void OpenUrlInBrowser(string url)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", url);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else
                Process.Start("xdg-open", url);
        }
        catch { }
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
