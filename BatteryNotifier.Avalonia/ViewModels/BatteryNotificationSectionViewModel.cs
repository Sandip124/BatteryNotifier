using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Services;
using ReactiveUI;

namespace BatteryNotifier.Avalonia.ViewModels;

public class BatteryNotificationSectionViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly Action<string?, int> _onSettingsChanged;

    private bool _isEnabled;
    private int _thresholdValue;
    private string? _soundPath;
    private SoundOption _selectedSound = SoundOption.Default;
    private bool _isCustomSound;
    private bool _disposed;

    public string Title { get; }
    public Bitmap? Icon { get; }
    public int SliderMinimum { get; }
    public int SliderMaximum { get; }
    public List<SoundOption> SoundOptions { get; } = SoundOption.BuildList();

    public Interaction<string?, string?> BrowseSoundInteraction { get; } = new();
    public ReactiveCommand<Unit, Unit> BrowseSoundCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetSoundCommand { get; }

    /// <param name="title">Section title, e.g. "Full Battery"</param>
    /// <param name="iconSource">Asset path, e.g. "/Assets/FullBattery.png"</param>
    /// <param name="sliderMin">Slider minimum</param>
    /// <param name="sliderMax">Slider maximum</param>
    /// <param name="isEnabled">Initial enabled state</param>
    /// <param name="thresholdValue">Initial threshold value</param>
    /// <param name="soundSettingsValue">Initial sound settings value (path or "builtin:...")</param>
    /// <param name="onSettingsChanged">Callback(soundSettingsValue, thresholdValue) to persist changes</param>
    public BatteryNotificationSectionViewModel(
        string title, string iconSource,
        int sliderMin, int sliderMax,
        bool isEnabled, int thresholdValue,
        string? soundSettingsValue,
        Action<string?, int> onSettingsChanged)
    {
        Title = title;
        Icon = LoadIcon(iconSource);
        SliderMinimum = sliderMin;
        SliderMaximum = sliderMax;
        _isEnabled = isEnabled;
        _thresholdValue = thresholdValue;
        _soundPath = BuiltInSounds.IsBuiltIn(soundSettingsValue) ? null : soundSettingsValue;
        _onSettingsChanged = onSettingsChanged;

        _selectedSound = SoundOption.FromSettingsValue(soundSettingsValue, SoundOptions);
        _isCustomSound = _selectedSound.IsCustom;

        BrowseSoundCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await BrowseSoundInteraction.Handle(SoundPath ?? string.Empty);
            if (result != null && ValidateSoundFilePath(result))
            {
                SoundPath = result;
                SelectedSound = SoundOptions.First(s => s.IsCustom);
                IsCustomSound = true;
                _onSettingsChanged(result, ThresholdValue);
            }
        });

        ResetSoundCommand = ReactiveCommand.Create(() =>
        {
            SoundPath = null;
            SelectedSound = SoundOptions.First(s => s.IsDefault);
            _onSettingsChanged(null, ThresholdValue);
        });

        this.WhenAnyValue(x => x.SelectedSound)
            .Skip(1)
            .Subscribe(option =>
            {
                if (option == null) return;
                IsCustomSound = option.IsCustom;
                if (option.IsCustom) return; // browse handles save
                SoundPath = null;
                _onSettingsChanged(option.SettingsValue, ThresholdValue);
            })
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.IsEnabled)
            .Skip(1)
            .Subscribe(_ => _onSettingsChanged(GetCurrentSoundValue(), ThresholdValue))
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.ThresholdValue)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(val => _onSettingsChanged(GetCurrentSoundValue(), val))
            .DisposeWith(_disposables);
    }

    private string? GetCurrentSoundValue()
    {
        if (IsCustomSound && !string.IsNullOrEmpty(SoundPath))
            return SoundPath;
        return SelectedSound?.SettingsValue;
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public int ThresholdValue
    {
        get => _thresholdValue;
        set => this.RaiseAndSetIfChanged(ref _thresholdValue, value);
    }

    public string? SoundPath
    {
        get => _soundPath;
        set => this.RaiseAndSetIfChanged(ref _soundPath, value);
    }

    public SoundOption SelectedSound
    {
        get => _selectedSound;
        set => this.RaiseAndSetIfChanged(ref _selectedSound, value);
    }

    public bool IsCustomSound
    {
        get => _isCustomSound;
        set => this.RaiseAndSetIfChanged(ref _isCustomSound, value);
    }

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".wav", ".mp3", ".m4a", ".wma", ".ogg", ".flac", ".aac" };

    private const long MaxSoundFileSizeBytes = 50 * 1024 * 1024; // 50 MB

    private static bool ValidateSoundFilePath(string path)
    {
        // Must be an absolute path
        if (!Path.IsPathRooted(path))
            return false;

        // Normalize to canonical form (handles / vs \ on Windows, .., symlink text)
        string canonical;
        try
        {
            canonical = Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }

        // Validate file extension
        var ext = Path.GetExtension(canonical);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return false;

        // Check file exists, is not a symlink, and size is within limit
        try
        {
            var info = new FileInfo(canonical);
            if (!info.Exists || info.Length > MaxSoundFileSizeBytes)
                return false;

            // Reject symlinks — could point to sensitive files
            if (info.LinkTarget != null)
                return false;
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static Bitmap? LoadIcon(string assetPath)
    {
        try
        {
            var uri = new Uri($"avares://BatteryNotifier{assetPath}");
            using var stream = AssetLoader.Open(uri);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposables.Dispose();
        _disposed = true;
    }
}
