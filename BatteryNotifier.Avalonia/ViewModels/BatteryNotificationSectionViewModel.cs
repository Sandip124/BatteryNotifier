using System;
using System.Collections.Generic;
using System.IO;
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
    private string? _soundSettingsValue;
    private string _soundDisplayName = "Default (none)";
    private bool _disposed;

    public string Title { get; }
    public Bitmap? Icon { get; }
    public int SliderMinimum { get; }
    public int SliderMaximum { get; }

    public Interaction<(string? SettingsValue, string Title), SoundPickerItem?> OpenSoundPickerInteraction { get; } = new();
    public ReactiveCommand<Unit, Unit> OpenSoundPickerCommand { get; }

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
        _soundSettingsValue = soundSettingsValue;
        _onSettingsChanged = onSettingsChanged;

        UpdateSoundDisplayName();

        OpenSoundPickerCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await OpenSoundPickerInteraction.Handle((_soundSettingsValue, Title));
            if (result != null)
            {
                string? newValue;
                if (result.IsCustom)
                {
                    newValue = result.CustomPath;
                    if (string.IsNullOrEmpty(newValue) || !ValidateSoundFilePath(newValue))
                        return;
                }
                else
                {
                    newValue = result.SettingsValue;
                }

                _soundSettingsValue = newValue;
                UpdateSoundDisplayName();
                _onSettingsChanged(newValue, ThresholdValue);
            }
        });

        this.WhenAnyValue(x => x.IsEnabled)
            .Skip(1)
            .Subscribe(_ => _onSettingsChanged(_soundSettingsValue, ThresholdValue))
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.ThresholdValue)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(val => _onSettingsChanged(_soundSettingsValue, val))
            .DisposeWith(_disposables);
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

    public string SoundDisplayName
    {
        get => _soundDisplayName;
        private set => this.RaiseAndSetIfChanged(ref _soundDisplayName, value);
    }

    private void UpdateSoundDisplayName()
    {
        if (string.IsNullOrEmpty(_soundSettingsValue))
        {
            SoundDisplayName = "Default (none)";
        }
        else if (BuiltInSounds.IsBuiltIn(_soundSettingsValue))
        {
            SoundDisplayName = BuiltInSounds.GetName(_soundSettingsValue) ?? "Unknown";
        }
        else
        {
            SoundDisplayName = Path.GetFileName(_soundSettingsValue) ?? "Custom file";
        }
    }

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".wav", ".mp3", ".m4a", ".wma", ".ogg", ".flac", ".aac" };

    private const long MaxSoundFileSizeBytes = 50 * 1024 * 1024;

    private static bool ValidateSoundFilePath(string path)
    {
        if (!Path.IsPathRooted(path))
            return false;

        string canonical;
        try
        {
            canonical = Path.GetFullPath(path);
        }
        catch
        {
            return false;
        }

        var ext = Path.GetExtension(canonical);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return false;

        try
        {
            var info = new FileInfo(canonical);
            if (!info.Exists || info.Length > MaxSoundFileSizeBytes)
                return false;

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
