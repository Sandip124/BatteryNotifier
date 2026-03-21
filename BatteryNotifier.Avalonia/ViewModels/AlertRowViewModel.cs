using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using BatteryNotifier.Avalonia.Services;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Models;
using ReactiveUI;
using Serilog;

namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class AlertRowViewModel : ViewModelBase, IDisposable
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("AlertRowViewModel");
    private readonly CompositeDisposable _disposables = new();
    private readonly BatteryAlert _alert;
    private readonly Action<bool> _onChanged;
    private bool _disposed;

    private string _label;
    private int _lowerBound;
    private int _upperBound;
    private bool _isEnabled;
    private string? _flashColor;

    public string Id => _alert.Id;
    public bool IsDefault => _alert.Id is "fullbatt" or "lowbatt_";
    public bool CanDelete => !IsDefault;

    /// <summary>Flash color options relevant to battery alert levels.</summary>
    public static IReadOnlyList<FlashColorOption> FlashColorOptions { get; } =
    [
        new("Auto", null),
        new("Red", "#D32F2F"),
        new("Amber", "#F57A00"),
        new("Green", "#388E3C"),
        new("Blue", "#0288D1"),
    ];

    public Interaction<(string? SettingsValue, string Title), SoundPickerItem?> OpenSoundPickerInteraction { get; } = new();
    public ReactiveCommand<Unit, Unit> OpenSoundPickerCommand { get; }
    public ReactiveCommand<Unit, Unit> PreviewCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

    public AlertRowViewModel(BatteryAlert alert, Action<bool> onChanged, Action<AlertRowViewModel> onDelete)
    {
        _alert = alert;
        _onChanged = onChanged;
        _label = alert.Label;
        _lowerBound = alert.LowerBound;
        _upperBound = alert.UpperBound;
        _isEnabled = alert.IsEnabled;
        _flashColor = alert.FlashColor;

        UpdateSoundDisplayName();

        OpenSoundPickerCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await OpenSoundPickerInteraction.Handle((_alert.Sound, _label));
            if (result != null)
            {
                _alert.Sound = result.SettingsValue;
                UpdateSoundDisplayName();
                _onChanged(false);
            }
        });

        PreviewCommand = ReactiveCommand.Create(PreviewAlert);
        DeleteCommand = ReactiveCommand.Create(() => onDelete(this));

        // Auto-save on property changes (throttled for sliders)
        this.WhenAnyValue(x => x.IsEnabled)
            .Skip(1)
            .Subscribe(_ => SyncAndSave())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.Label)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => SyncAndSave())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.LowerBound, x => x.UpperBound)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => SyncAndSave())
            .DisposeWith(_disposables);

        this.WhenAnyValue(x => x.FlashColor)
            .Skip(1)
            .Subscribe(_ => SyncAndSave())
            .DisposeWith(_disposables);
    }

    private void SyncAndSave()
    {
        var rangeChanged = _alert.LowerBound != _lowerBound || _alert.UpperBound != _upperBound;

        _alert.Label = _label;
        _alert.LowerBound = _lowerBound;
        _alert.UpperBound = _upperBound;
        _alert.IsEnabled = _isEnabled;
        _alert.FlashColor = _flashColor;

        if (rangeChanged)
            Logger.Information("Alert '{Label}' ({Id}) range changed to {Lower}%–{Upper}%",
                _label, _alert.Id, _lowerBound, _upperBound);

        _onChanged(rangeChanged);
    }

    public string Label
    {
        get => _label;
        set => this.RaiseAndSetIfChanged(ref _label, value);
    }

    public int LowerBound
    {
        get => _lowerBound;
        set => this.RaiseAndSetIfChanged(ref _lowerBound, value);
    }

    public int UpperBound
    {
        get => _upperBound;
        set => this.RaiseAndSetIfChanged(ref _upperBound, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }

    public string? FlashColor
    {
        get => _flashColor;
        set
        {
            if (this.RaiseAndSetIfChanged(ref _flashColor, value) != value) return;
            this.RaisePropertyChanged(nameof(HasFlashColor));
            this.RaisePropertyChanged(nameof(SelectedFlashColorOption));
        }
    }

    public bool HasFlashColor => !string.IsNullOrEmpty(_flashColor);

    public FlashColorOption SelectedFlashColorOption
    {
        get => FlashColorOptions.FirstOrDefault(o =>
            string.Equals(o.Hex, _flashColor, StringComparison.OrdinalIgnoreCase))
            ?? FlashColorOptions[0];
        set => FlashColor = value.Hex;
    }

    public string RangeDescription => $"{LowerBound}% – {UpperBound}%";

    public string SoundDisplayName
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Default (none)";

    private void UpdateSoundDisplayName()
    {
        var sound = _alert.Sound;
        if (string.IsNullOrEmpty(sound))
            SoundDisplayName = "Default (none)";
        else if (BuiltInSounds.IsBuiltIn(sound))
            SoundDisplayName = BuiltInSounds.GetName(sound) ?? "Unknown";
        else if (CustomSoundsLibrary.IsCustom(sound))
        {
            var fileName = CustomSoundsLibrary.GetFileName(sound);
            SoundDisplayName = fileName != null ? Path.GetFileNameWithoutExtension(fileName) : "Custom sound";
        }
        else if (BundledSounds.IsBundled(sound))
        {
            var fileName = BundledSounds.GetFileName(sound);
            SoundDisplayName = fileName != null ? Path.GetFileNameWithoutExtension(fileName) : "Bundled sound";
        }
        else
            SoundDisplayName = Path.GetFileName(sound) ?? "Custom file";
    }

    private void PreviewAlert()
    {
        var displayService = NotificationDisplayService.Current;
        if (displayService == null) return;

        var notification = new Core.Services.NotificationMessageEventArgs
        {
            Message = $"Preview — {_label} ({_lowerBound}%–{_upperBound}%)",
            Tag = _alert.Id
        };

        displayService.ShowNotification(notification, _alert, playSound: true);
    }

    public BatteryAlert GetAlert() => _alert;

    public void Dispose()
    {
        if (_disposed) return;
        _disposables.Dispose();
        _disposed = true;
    }
}

public sealed class FlashColorOption
{
    public string Name { get; }
    public string? Hex { get; }
    public IBrush PreviewBrush { get; }

    public FlashColorOption(string name, string? hex)
    {
        Name = name;
        Hex = hex;
        PreviewBrush = hex != null
            ? new SolidColorBrush(Color.Parse(hex))
            : new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Colors.Red, 0),
                    new GradientStop(Colors.Orange, 0.33),
                    new GradientStop(Colors.Green, 0.66),
                    new GradientStop(Colors.Blue, 1),
                }
            };
    }

    // Equality by Hex so ComboBox SelectedItem matching works
    public override bool Equals(object? obj) => obj is FlashColorOption o &&
        string.Equals(Hex, o.Hex, StringComparison.OrdinalIgnoreCase);
    public override int GetHashCode() => (Hex?.ToUpperInvariant() ?? "").GetHashCode();
}
