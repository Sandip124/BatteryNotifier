using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using BatteryNotifier.Core.Services;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings = AppSettings.Instance;

    private bool _isSystemTheme;
    private bool _isLightTheme;
    private bool _isDarkTheme;
    private bool _pinToWindow;
    private bool _startMinimized;
    private bool _launchAtStartup;
    private bool _fullBatteryNotification;
    private bool _lowBatteryNotification;
    private int _fullBatteryNotificationValue;
    private int _lowBatteryNotificationValue;
    private string? _fullBatterySoundPath;
    private string? _lowBatterySoundPath;

    public Interaction<string?, string?> BrowseFullBatterySoundInteraction { get; } = new();
    public Interaction<string?, string?> BrowseLowBatterySoundInteraction { get; } = new();

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> SetSystemThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> SetLightThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> SetDarkThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseFullBatterySoundCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseLowBatterySoundCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetFullBatterySoundCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetLowBatterySoundCommand { get; }

    public event EventHandler<bool>? PinToWindowChanged;

    public SettingsViewModel(Action navigateBack)
    {
        BackCommand = ReactiveCommand.Create(navigateBack);

        LoadSettings();

        SetSystemThemeCommand = ReactiveCommand.Create(ApplySystemTheme);
        SetLightThemeCommand = ReactiveCommand.Create(ApplyLightTheme);
        SetDarkThemeCommand = ReactiveCommand.Create(ApplyDarkTheme);

        BrowseFullBatterySoundCommand = ReactiveCommand.CreateFromTask(BrowseFullBatterySound);
        BrowseLowBatterySoundCommand = ReactiveCommand.CreateFromTask(BrowseLowBatterySound);

        ResetFullBatterySoundCommand = ReactiveCommand.Create(() =>
        {
            FullBatterySoundPath = null;
            _settings.FullBatteryNotificationMusic = null;
            _settings.Save();
        });

        ResetLowBatterySoundCommand = ReactiveCommand.Create(() =>
        {
            LowBatterySoundPath = null;
            _settings.LowBatteryNotificationMusic = null;
            _settings.Save();
        });

        this.WhenAnyValue(x => x.LaunchAtStartup)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.LaunchAtStartup = enabled;
                _settings.Save();
                StartupManager.SetStartup(enabled);
            });

        this.WhenAnyValue(x => x.PinToWindow)
            .Skip(1)
            .Subscribe(pinned =>
            {
                _settings.PinToWindow = pinned;
                _settings.Save();
                PinToWindowChanged?.Invoke(this, pinned);
            });

        this.WhenAnyValue(x => x.StartMinimized)
            .Skip(1)
            .Subscribe(minimized =>
            {
                _settings.StartMinimized = minimized;
                _settings.Save();
            });

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

        this.WhenAnyValue(x => x.FullBatteryNotificationValue)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(value =>
            {
                _settings.FullBatteryNotificationValue = value;
                _settings.Save();
                BatteryMonitorService.Instance.SetThresholds(
                    _settings.LowBatteryNotificationValue,
                    _settings.FullBatteryNotificationValue);
            });

        this.WhenAnyValue(x => x.LowBatteryNotificationValue)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(value =>
            {
                _settings.LowBatteryNotificationValue = value;
                _settings.Save();
                BatteryMonitorService.Instance.SetThresholds(
                    _settings.LowBatteryNotificationValue,
                    _settings.FullBatteryNotificationValue);
            });
    }

    private void LoadSettings()
    {
        _isSystemTheme = _settings.ThemeMode == ThemeMode.System;
        _isLightTheme = _settings.ThemeMode == ThemeMode.Light;
        _isDarkTheme = _settings.ThemeMode == ThemeMode.Dark;
        _pinToWindow = _settings.PinToWindow;
        _startMinimized = _settings.StartMinimized;
        _launchAtStartup = _settings.LaunchAtStartup;
        _fullBatteryNotification = _settings.FullBatteryNotification;
        _lowBatteryNotification = _settings.LowBatteryNotification;
        _fullBatteryNotificationValue = _settings.FullBatteryNotificationValue;
        _lowBatteryNotificationValue = _settings.LowBatteryNotificationValue;
        _fullBatterySoundPath = _settings.FullBatteryNotificationMusic;
        _lowBatterySoundPath = _settings.LowBatteryNotificationMusic;
    }

    private void ApplySystemTheme()
    {
        _settings.ThemeMode = ThemeMode.System;
        _settings.Save();
        IsSystemTheme = true;
        IsLightTheme = false;
        IsDarkTheme = false;
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = ThemeVariant.Default;
    }

    private void ApplyLightTheme()
    {
        _settings.ThemeMode = ThemeMode.Light;
        _settings.Save();
        IsSystemTheme = false;
        IsLightTheme = true;
        IsDarkTheme = false;
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = ThemeVariant.Light;
    }

    private void ApplyDarkTheme()
    {
        _settings.ThemeMode = ThemeMode.Dark;
        _settings.Save();
        IsSystemTheme = false;
        IsLightTheme = false;
        IsDarkTheme = true;
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
    }

    private async Task BrowseFullBatterySound()
    {
        var result = await BrowseFullBatterySoundInteraction.Handle(
            _settings.FullBatteryNotificationMusic ?? string.Empty);
        if (result != null)
        {
            FullBatterySoundPath = result;
            _settings.FullBatteryNotificationMusic = result;
            _settings.Save();
        }
    }

    private async Task BrowseLowBatterySound()
    {
        var result = await BrowseLowBatterySoundInteraction.Handle(
            _settings.LowBatteryNotificationMusic ?? string.Empty);
        if (result != null)
        {
            LowBatterySoundPath = result;
            _settings.LowBatteryNotificationMusic = result;
            _settings.Save();
        }
    }

    public bool IsSystemTheme
    {
        get => _isSystemTheme;
        set => this.RaiseAndSetIfChanged(ref _isSystemTheme, value);
    }

    public bool IsLightTheme
    {
        get => _isLightTheme;
        set => this.RaiseAndSetIfChanged(ref _isLightTheme, value);
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set => this.RaiseAndSetIfChanged(ref _isDarkTheme, value);
    }

    public bool PinToWindow
    {
        get => _pinToWindow;
        set => this.RaiseAndSetIfChanged(ref _pinToWindow, value);
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set => this.RaiseAndSetIfChanged(ref _startMinimized, value);
    }

    public bool LaunchAtStartup
    {
        get => _launchAtStartup;
        set => this.RaiseAndSetIfChanged(ref _launchAtStartup, value);
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

    public int FullBatteryNotificationValue
    {
        get => _fullBatteryNotificationValue;
        set => this.RaiseAndSetIfChanged(ref _fullBatteryNotificationValue, value);
    }

    public int LowBatteryNotificationValue
    {
        get => _lowBatteryNotificationValue;
        set => this.RaiseAndSetIfChanged(ref _lowBatteryNotificationValue, value);
    }

    public string? FullBatterySoundPath
    {
        get => _fullBatterySoundPath;
        set => this.RaiseAndSetIfChanged(ref _fullBatterySoundPath, value);
    }

    public string? LowBatterySoundPath
    {
        get => _lowBatterySoundPath;
        set => this.RaiseAndSetIfChanged(ref _lowBatterySoundPath, value);
    }
}