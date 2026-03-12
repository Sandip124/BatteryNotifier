using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Styling;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Services;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly AppSettings _settings = AppSettings.Instance;
    private readonly CompositeDisposable _disposables = new();

    private bool _isSystemTheme;
    private bool _isLightTheme;
    private bool _isDarkTheme;
    private bool _launchAtStartup;
    private bool _disposed;

    public ReactiveCommand<Unit, Unit> BackCommand { get; }
    public ReactiveCommand<Unit, Unit> SetSystemThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> SetLightThemeCommand { get; }
    public ReactiveCommand<Unit, Unit> SetDarkThemeCommand { get; }

    public BatteryNotificationSectionViewModel FullBatterySection { get; }
    public BatteryNotificationSectionViewModel LowBatterySection { get; }

    public SettingsViewModel(Action navigateBack)
    {
        BackCommand = ReactiveCommand.Create(navigateBack);

        LoadSettings();

        SetSystemThemeCommand = ReactiveCommand.Create(ApplySystemTheme);
        SetLightThemeCommand = ReactiveCommand.Create(ApplyLightTheme);
        SetDarkThemeCommand = ReactiveCommand.Create(ApplyDarkTheme);

        FullBatterySection = new BatteryNotificationSectionViewModel(
            title: "Full Battery",
            iconSource: "/Assets/FullBattery.png",
            sliderMin: 50, sliderMax: 100,
            isEnabled: _settings.FullBatteryNotification,
            thresholdValue: _settings.FullBatteryNotificationValue,
            soundSettingsValue: _settings.FullBatteryNotificationMusic,
            onSettingsChanged: (sound, threshold) =>
            {
                _settings.FullBatteryNotification = FullBatterySection.IsEnabled;
                _settings.FullBatteryNotificationValue = threshold;
                _settings.FullBatteryNotificationMusic = sound;
                _settings.Save();
                UpdateThresholds();
            });

        LowBatterySection = new BatteryNotificationSectionViewModel(
            title: "Low Battery",
            iconSource: "/Assets/LowBattery.png",
            sliderMin: 5, sliderMax: 50,
            isEnabled: _settings.LowBatteryNotification,
            thresholdValue: _settings.LowBatteryNotificationValue,
            soundSettingsValue: _settings.LowBatteryNotificationMusic,
            onSettingsChanged: (sound, threshold) =>
            {
                _settings.LowBatteryNotification = LowBatterySection.IsEnabled;
                _settings.LowBatteryNotificationValue = threshold;
                _settings.LowBatteryNotificationMusic = sound;
                _settings.Save();
                UpdateThresholds();
            });

        this.WhenAnyValue(x => x.LaunchAtStartup)
            .Skip(1)
            .Subscribe(enabled =>
            {
                _settings.LaunchAtStartup = enabled;
                _settings.Save();
                StartupManager.SetStartup(enabled);
            })
            .DisposeWith(_disposables);

    }

    private void UpdateThresholds()
    {
        try
        {
            BatteryMonitorService.Instance.SetThresholds(
                LowBatterySection.ThresholdValue,
                FullBatterySection.ThresholdValue);
        }
        catch { }
    }

    private void LoadSettings()
    {
        _isSystemTheme = _settings.ThemeMode == ThemeMode.System;
        _isLightTheme = _settings.ThemeMode == ThemeMode.Light;
        _isDarkTheme = _settings.ThemeMode == ThemeMode.Dark;
        _launchAtStartup = _settings.LaunchAtStartup;
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

    public void Dispose()
    {
        if (_disposed) return;
        FullBatterySection.Dispose();
        LowBatterySection.Dispose();
        _disposables.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Represents a sound option in the settings UI (built-in, custom, or default/none).
/// </summary>
public class SoundOption
{
    public string DisplayName { get; init; } = string.Empty;
    public string? SettingsValue { get; init; }
    public bool IsDefault { get; init; }
    public bool IsCustom { get; init; }

    public static readonly SoundOption Default = new() { DisplayName = "Default (none)", IsDefault = true };
    public static readonly SoundOption Custom = new() { DisplayName = "Custom file...", IsCustom = true };

    public static List<SoundOption> BuildList()
    {
        var list = new List<SoundOption> { Default };

        foreach (var name in BuiltInSounds.Names)
        {
            list.Add(new SoundOption
            {
                DisplayName = name,
                SettingsValue = BuiltInSounds.ToSettingsValue(name)
            });
        }

        list.Add(Custom);
        return list;
    }

    public static SoundOption FromSettingsValue(string? value, List<SoundOption> options)
    {
        if (string.IsNullOrEmpty(value))
            return options.First(o => o.IsDefault);

        if (BuiltInSounds.IsBuiltIn(value))
            return options.FirstOrDefault(o => o.SettingsValue == value) ?? options.First(o => o.IsDefault);

        // Custom file path
        return options.First(o => o.IsCustom);
    }

    public override string ToString() => DisplayName;
}
