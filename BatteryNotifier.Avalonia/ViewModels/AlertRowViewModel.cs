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
using BatteryNotifier.Core.Services;
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

    private bool _isEditingLabel;

    public bool IsEditingLabel
    {
        get => _isEditingLabel;
        set
        {
            this.RaiseAndSetIfChanged(ref _isEditingLabel, value);
            this.RaisePropertyChanged(nameof(ShowLabelDisplay));
            this.RaisePropertyChanged(nameof(ShowLabelEditor));
        }
    }

    public bool ShowLabelDisplay => CanDelete && !_isEditingLabel;
    public bool ShowLabelEditor => CanDelete && _isEditingLabel;

    /// <summary>Flash color palette for battery alerts.</summary>
    public static IReadOnlyList<FlashColorOption> FlashColorOptions { get; } =
    [
        new("Red", AlertAccent.RedHex),
        new("Amber", AlertAccent.AmberHex),
        new("Green", AlertAccent.GreenHex),
        new("Blue", AlertAccent.BlueHex),
        new("Purple", AlertAccent.PurpleHex),
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
                if (AppSettings.Instance.ScreenFlashEnabled)
                    FlashSequenceLibrary.Instance.EnsureGenerated(result.SettingsValue);
                UpdateSoundDisplayName();
                _onChanged(false);
            }
        });

        PreviewCommand = ReactiveCommand.Create(TogglePreview);
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
        {
            Logger.Information("Alert '{Label}' ({Id}) range changed to {Lower}%–{Upper}%",
                _label, _alert.Id, _lowerBound, _upperBound);
            RaiseAccentChanged(); // tone (and thus the auto tint) can shift with the range
        }

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
            RaiseAccentChanged();
        }
    }

    public bool HasFlashColor => !string.IsNullOrEmpty(_flashColor);

    // ── Card accent tint (reflects the chosen flash color, else the auto tone) ──

    /// <summary>The card's accent color: the chosen flash color, or the alert's tone color.</summary>
    public Color AccentColorValue
    {
        get
        {
            if (!string.IsNullOrEmpty(_flashColor))
            {
                try { return Color.Parse(_flashColor); }
                catch { /* fall through to the tone color */ }
            }

            return _alert.Tone switch
            {
                AlertTone.Full => AlertAccent.Green,
                AlertTone.Low => AlertAccent.Red,
                _ => Color.Parse("#8A8A8A"),
            };
        }
    }

    /// <summary>Radial accent wash emanating from the top-left header and fading across the card.</summary>
    public IBrush CardTintBrush
    {
        get
        {
            var c = AccentColorValue;
            return new RadialGradientBrush
            {
                Center = new RelativePoint(0, 0, RelativeUnit.Relative),
                GradientOrigin = new RelativePoint(0, 0, RelativeUnit.Relative),
                RadiusX = new RelativeScalar(1.2, RelativeUnit.Relative),
                RadiusY = new RelativeScalar(1.2, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0x33, c.R, c.G, c.B), 0),
                    new GradientStop(Color.FromArgb(0x10, c.R, c.G, c.B), 0.5),
                    new GradientStop(Color.FromArgb(0x00, c.R, c.G, c.B), 1),
                }
            };
        }
    }

    public IBrush CardBorderBrush => new SolidColorBrush(AccentColorValue, 0.30);

    private void RaiseAccentChanged()
    {
        this.RaisePropertyChanged(nameof(AccentColorValue));
        this.RaisePropertyChanged(nameof(CardTintBrush));
        this.RaisePropertyChanged(nameof(CardBorderBrush));
    }

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

    /// <summary>True while this alert's preview is showing — drives the play/stop toggle button.</summary>
    public bool IsPreviewing
    {
        get;
        private set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(PreviewButtonText));
        }
    }

    public string PreviewButtonText => IsPreviewing ? "Stop" : "Preview";

    private void TogglePreview()
    {
        var displayService = NotificationDisplayService.Current;
        if (displayService == null) return;

        if (IsPreviewing)
        {
            displayService.DismissAll(); // stops card + sound + flash; Closed handler clears IsPreviewing
            return;
        }

        var notification = new Core.Services.NotificationMessageEventArgs
        {
            Message = $"Preview — {_label} ({_lowerBound}%–{_upperBound}%)",
            Tag = _alert.Id
        };

        IsPreviewing = true;
        displayService.ShowNotification(notification, _alert, playSound: true,
            onClosed: () => IsPreviewing = false);
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
                // "Auto" swatch previews the accent palette as a gradient.
                GradientStops =
                {
                    new GradientStop(AlertAccent.Red, 0),
                    new GradientStop(AlertAccent.Amber, 0.33),
                    new GradientStop(AlertAccent.Green, 0.66),
                    new GradientStop(AlertAccent.Blue, 1),
                }
            };
    }

    // Equality by Hex so ComboBox SelectedItem matching works
    public override bool Equals(object? obj) => obj is FlashColorOption o &&
        string.Equals(Hex, o.Hex, StringComparison.OrdinalIgnoreCase);
    public override int GetHashCode() => (Hex?.ToUpperInvariant() ?? "").GetHashCode();
}
