using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.Services;
using BatteryNotifier.Core.Managers;
using BatteryNotifier.Core.Services;
using ReactiveUI;
namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class SoundPickerViewModel : ViewModelBase, IDisposable
{
    private readonly SoundManager _soundManager = new();
    private readonly CompositeDisposable _disposables = new();
    private readonly string? _currentSettingsValue;
    private List<SoundPickerGroup> _allGroups;
    private SoundPickerItem? _playingItem;

    private bool _disposed;

    public string PickerTitle { get; }
    public ReactiveCommand<Unit, SoundPickerItem?> SelectCommand { get; }
    public ReactiveCommand<Unit, SoundPickerItem?> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportSoundCommand { get; }
    public ReactiveCommand<SoundPickerItem, Unit> DeleteCustomCommand { get; }

    public Interaction<Unit, string?> BrowseFileInteraction { get; } = new();

    private const int SelectButtonNameMaxLength = 16;

    public SoundPickerItem? SelectedItem
    {
        get;
        set
        {
            if (field == value) return;
            if (field != null) field.IsSelected = false;
            field = value;
            if (value != null) value.IsSelected = true;
            this.RaisePropertyChanged();
            this.RaisePropertyChanged(nameof(SelectButtonText));
        }
    }

    /// <summary>Plain "Select" until a sound is clicked, then "Select '&lt;name&gt;'" (truncated).</summary>
    public string SelectButtonText => SelectedItem is { } item
        ? $"Select '{TruncateName(item.DisplayName)}'"
        : "Select";

    private static string TruncateName(string name) =>
        name.Length <= SelectButtonNameMaxLength
            ? name
            : name[..(SelectButtonNameMaxLength - 1)] + "…";

    public string? SearchText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public List<SoundPickerGroup> FilteredGroups
    {
        get;
        private set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(HasNoResults));
        }
    } = [];

    public bool HasNoResults => FilteredGroups.Count == 0;

    public SoundPickerViewModel(string? currentSettingsValue, string sectionTitle)
    {
        PickerTitle = $"Choose {sectionTitle} Sound";
        _currentSettingsValue = currentSettingsValue;
        _allGroups = BuildGroups();
        MarkCurrent();
        ApplyFilter(null);

        var canSelect = this.WhenAnyValue(x => x.SelectedItem)
            .Select(item => item != null);
        SelectCommand = ReactiveCommand.Create(() => SelectedItem, canSelect);
        CancelCommand = ReactiveCommand.Create(() => (SoundPickerItem?)null);

        ImportSoundCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var path = await BrowseFileInteraction.Handle(Unit.Default);
            if (string.IsNullOrEmpty(path)) return;

            var fileName = CustomSoundsLibrary.Import(path);
            if (fileName == null) return;

            _allGroups = BuildGroups();
            MarkCurrent();
            ApplyFilter(SearchText);

            var settingsValue = CustomSoundsLibrary.ToSettingsValue(fileName);
            FlashSequenceLibrary.Instance.Invalidate(settingsValue); 
            if (AppSettings.Instance.ScreenFlashEnabled)
                FlashSequenceLibrary.Instance.EnsureGenerated(settingsValue);
            SelectedItem = _allGroups
                .SelectMany(g => g.Items)
                .FirstOrDefault(i => i.SettingsValue == settingsValue);
        });

        DeleteCustomCommand = ReactiveCommand.Create<SoundPickerItem>(item =>
        {
            var fileName = CustomSoundsLibrary.GetFileName(item.SettingsValue);
            if (fileName == null) return;

            CustomSoundsLibrary.Delete(fileName);
            FlashSequenceLibrary.Instance.Invalidate(item.SettingsValue);

            if (SelectedItem == item)
                SelectedItem = null;

            _allGroups = BuildGroups();
            MarkCurrent();
            ApplyFilter(SearchText);
        });

        this.WhenAnyValue(x => x.SearchText)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(150))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ApplyFilter)
            .DisposeWith(_disposables);
    }

    /// <summary>Flags the item matching the saved value as current (for the "Selected" badge).</summary>
    private void MarkCurrent()
    {
        foreach (var item in _allGroups.SelectMany(g => g.Items))
            item.IsCurrent = !string.IsNullOrEmpty(_currentSettingsValue)
                && string.Equals(item.SettingsValue, _currentSettingsValue, StringComparison.Ordinal);
    }

    private void ApplyFilter(string? search)
    {
        var filtered = new List<SoundPickerGroup>();
        var hasSearch = !string.IsNullOrWhiteSpace(search);

        foreach (var group in _allGroups)
        {
            var items = hasSearch
                ? group.Items.Where(i => i.DisplayName.Contains(search!, StringComparison.OrdinalIgnoreCase)).ToList()
                : group.Items.ToList();

            if (items.Count > 0)
                filtered.Add(new SoundPickerGroup(group.Title, items));
        }

        FilteredGroups = filtered;
    }

    /// <summary>
    /// Toggles preview playback for the item on demand (from its play/pause button)
    /// </summary>
    public void TogglePreview(SoundPickerItem item)
    {
        if (item.IsPlaying)
        {
            StopPreview();
            return;
        }

        StopPreview();

        _playingItem = item;
        item.IsPlaying = true;
        _ = PlayThenResetAsync(item);
    }

    private async Task PlayThenResetAsync(SoundPickerItem item)
    {
        try
        {
            await PreviewItem(item).ConfigureAwait(false);
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_playingItem == item)
                {
                    item.IsPlaying = false;
                    _playingItem = null;
                }
            });
        }
    }

    public async Task PreviewItem(SoundPickerItem item)
    {
        _soundManager.StopSound();
        await Task.Delay(100).ConfigureAwait(false);

        var source = item.SettingsValue;
        if (string.IsNullOrEmpty(source)) return;

        bool isShortTone = BuiltInSounds.IsBuiltIn(source);
        int previewMs = isShortTone ? 5000 : 60_000;
        await _soundManager.PlaySoundAsync(source, loop: false, durationMs: previewMs).ConfigureAwait(false);
    }

    public void StopPreview()
    {
        if (_playingItem != null)
        {
            _playingItem.IsPlaying = false;
            _playingItem = null;
        }

        try { _soundManager.StopSound(); }
        catch { /* best effort */ }
    }

    private static List<SoundPickerGroup> BuildGroups()
    {
        var groups = new List<SoundPickerGroup>
        {
            new("Full Battery — Calm",
            [
                new("Zen", BuiltInSounds.ToSettingsValue("Zen")),
                new("Harp", BuiltInSounds.ToSettingsValue("Harp")),
                new("Breeze", BuiltInSounds.ToSettingsValue("Breeze")),
                new("Bloom", BuiltInSounds.ToSettingsValue("Bloom")),
            ]),
            new("Low Battery — Warning",
            [
                new("Pulse", BuiltInSounds.ToSettingsValue("Pulse")),
                new("Klaxon", BuiltInSounds.ToSettingsValue("Klaxon")),
                new("Rattle", BuiltInSounds.ToSettingsValue("Rattle")),
            ]),
            new("General",
            [
                new("Chime", BuiltInSounds.ToSettingsValue("Chime")),
                new("Alert", BuiltInSounds.ToSettingsValue("Alert")),
                new("Beacon", BuiltInSounds.ToSettingsValue("Beacon")),
            ]),
        };

        // Add bundled "Editor's Choice" sounds grouped by category
        var catalog = BundledSounds.GetCatalog();
        var bundledByCategory = catalog
            .GroupBy(s => s.Category)
            .Select(g => new SoundPickerGroup(
                $"Editor's Choice — {g.Key}",
                g.Select(s => new SoundPickerItem(s.Name, s.SettingsValue)).ToList()))
            .ToList();
        groups.AddRange(bundledByCategory);

        // Add custom library sounds if any exist
        var customFiles = CustomSoundsLibrary.ListAll();
        if (customFiles.Count > 0)
        {
            var customItems = customFiles
                .Select(f => new SoundPickerItem(
                    System.IO.Path.GetFileNameWithoutExtension(f),
                    CustomSoundsLibrary.ToSettingsValue(f)) { IsCustomLibraryItem = true })
                .ToList();
            groups.Add(new SoundPickerGroup("Custom", customItems));
        }

        return groups;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopPreview();
        _soundManager.Dispose();
        _disposables.Dispose();
    }
}

public sealed class SoundPickerGroup(string title, List<SoundPickerItem> items)
{
    public string Title { get; } = title;
    public List<SoundPickerItem> Items { get; } = items;
}

public sealed class SoundPickerItem : ReactiveObject
{
    public string Name { get; }
    public string? SettingsValue { get; }
    public bool IsCustomLibraryItem { get; init; }

    public string DisplayName => Name;

    /// <summary>True while this item's preview is playing (toggles the play/pause icon).</summary>
    public bool IsPlaying
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>Tentative highlight for the row the user clicked (what the Select button will apply).</summary>
    public bool IsSelected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>True for the currently-saved sound — drives the persistent "Selected" badge.</summary>
    public bool IsCurrent
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public SoundPickerItem(string name, string? settingsValue)
    {
        Name = name;
        SettingsValue = settingsValue;
    }
}
