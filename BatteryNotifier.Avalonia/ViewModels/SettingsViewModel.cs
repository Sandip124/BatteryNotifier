using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Styling;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly AppSettings _settings = AppSettings.Instance;
    private readonly CompositeDisposable _disposables = new();

    private bool _isSystemTheme;
    private bool _isLightTheme;
    private bool _isDarkTheme;
    private bool _launchAtStartup;
    private bool _autoCheckForUpdates;
    private bool _screenFlashEnabled;
    private NotificationPosition _notificationPosition;
    private bool _disposed;

    private const int MaxAlerts = 5;

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> SetSystemThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> SetLightThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> SetDarkThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> AddAlertCommand { get; }
    public ReactiveCommand<NotificationPosition, Unit> SetPositionCommand { get; }

    public ObservableCollection<AlertRowViewModel> Alerts { get; } = new();

    public SettingsViewModel(Action navigateBack)
    {
        BackCommand = ReactiveCommand.Create(navigateBack);

        LoadSettings();
        LoadAlerts();

        SetSystemThemeCommand = ReactiveCommand.Create(ApplySystemTheme);
        SetLightThemeCommand = ReactiveCommand.Create(ApplyLightTheme);
        SetDarkThemeCommand = ReactiveCommand.Create(ApplyDarkTheme);

        var canAddAlert = this.WhenAnyValue(x => x.Alerts.Count)
            .Select(count => count < MaxAlerts);
        AddAlertCommand = ReactiveCommand.Create(AddAlert, canAddAlert);
        SetPositionCommand = ReactiveCommand.Create<NotificationPosition>(pos => NotificationPosition = pos);

        this.WhenAnyValue(x => x.LaunchAtStartup)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.LaunchAtStartup = enabled;
                _settings.Save();
                StartupManager.SetStartup(enabled);
            })
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.AutoCheckForUpdates)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.AutoCheckForUpdates = enabled;
                _settings.Save();
            })
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.ScreenFlashEnabled)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.ScreenFlashEnabled = enabled;
                _settings.Save();
            })
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.NotificationPosition)
            .Skip(1)
            .Subscribe(pos =>
            {
                _settings.NotificationPosition = pos;
                _settings.Save();
            })
            .DisposeWith(_disposables);
    }

    private void LoadAlerts()
    {
        foreach (var alert in _settings.Alerts)
        {
            Alerts.Add(CreateAlertRow(alert));
        }
    }

    private AlertRowViewModel CreateAlertRow(BatteryAlert alert)
    {
        return new AlertRowViewModel(alert, SaveAlerts, DeleteAlert);
    }

    private void AddAlert()
    {
        if (Alerts.Count >= MaxAlerts) return;

        var alert = new BatteryAlert
        {
            Label = $"Alert {Alerts.Count + 1}",
            LowerBound = 20,
            UpperBound = 80,
            IsEnabled = true,
            Sound = "builtin:Harp"
        };

        _settings.Alerts.Add(alert);
        Alerts.Add(CreateAlertRow(alert));
        SaveAlerts();
        this.RaisePropertyChanged(nameof(CanAddAlert));
    }

    private void DeleteAlert(AlertRowViewModel row)
    {
        _settings.Alerts.RemoveAll(a => a.Id == row.Id);
        Alerts.Remove(row);
        row.Dispose();
        SaveAlerts();
        this.RaisePropertyChanged(nameof(CanAddAlert));
    }

    private void SaveAlerts()
    {
        _settings.Save();
    }

    public bool CanAddAlert => Alerts.Count < MaxAlerts;

    private void LoadSettings()
    {
        _isSystemTheme = _settings.ThemeMode == ThemeMode.System;
        _isLightTheme = _settings.ThemeMode == ThemeMode.Light;
        _isDarkTheme = _settings.ThemeMode == ThemeMode.Dark;
        _launchAtStartup = _settings.LaunchAtStartup;
        _autoCheckForUpdates = _settings.AutoCheckForUpdates;
        _screenFlashEnabled = _settings.ScreenFlashEnabled;
        _notificationPosition = _settings.NotificationPosition;
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

    public bool LaunchAtStartup
    {
        get => _launchAtStartup;
        set => this.RaiseAndSetIfChanged(ref _launchAtStartup, value);
    }

    public bool AutoCheckForUpdates
    {
        get => _autoCheckForUpdates;
        set => this.RaiseAndSetIfChanged(ref _autoCheckForUpdates, value);
    }

    public bool ScreenFlashEnabled
    {
        get => _screenFlashEnabled;
        set => this.RaiseAndSetIfChanged(ref _screenFlashEnabled, value);
    }

    public NotificationPosition NotificationPosition
    {
        get => _notificationPosition;
        set => this.RaiseAndSetIfChanged(ref _notificationPosition, value);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var alert in Alerts)
        {
            try { alert.Dispose(); }
            catch { /* ensure remaining alerts are still disposed */ }
        }
        _disposables.Dispose();
    }
}
