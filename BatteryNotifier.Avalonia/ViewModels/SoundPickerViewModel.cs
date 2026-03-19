using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using BatteryNotifier.Avalonia.Services;
using BatteryNotifier.Core.Managers;
using ReactiveUI;
namespace BatteryNotifier.Avalonia.ViewModels;

public sealed class SoundPickerViewModel : ViewModelBase, IDisposable
{
    private readonly SoundManager _soundManager = new();
    private readonly CompositeDisposable _disposables = new();
    private List<SoundPickerGroup> _allGroups;

    private bool _disposed;

    public string PickerTitle { get; }
    public ReactiveCommand<Unit, SoundPickerItem?> SelectCommand { get; }
    public ReactiveCommand<Unit, SoundPickerItem?> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportSoundCommand { get; }
    public ReactiveCommand<SoundPickerItem, Unit> DeleteCustomCommand { get; }

    public Interaction<Unit, string?> BrowseFileInteraction { get; } = new();
    public event Action? FilterChanged;

    public SoundPickerItem? SelectedItem
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string? SearchText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public List<SoundPickerGroup> FilteredGroups
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public SoundPickerViewModel(string? currentSettingsValue, string sectionTitle)
    {
        PickerTitle = $"Choose {sectionTitle} Sound";
        _allGroups = BuildGroups();

        // Set initial selection
        if (!string.IsNullOrEmpty(currentSettingsValue))
        {
            SelectedItem = _allGroups
                .SelectMany(g => g.Items)
                .FirstOrDefault(i => string.Equals(i.SettingsValue, currentSettingsValue, StringComparison.Ordinal));
        }

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

            // Rebuild groups to include the new import
            _allGroups = BuildGroups();
            ApplyFilter(SearchText);
            FilterChanged?.Invoke();

            // Auto-select the newly imported sound
            var settingsValue = CustomSoundsLibrary.ToSettingsValue(fileName);
            SelectedItem = _allGroups
                .SelectMany(g => g.Items)
                .FirstOrDefault(i => i.SettingsValue == settingsValue);
        });

        DeleteCustomCommand = ReactiveCommand.Create<SoundPickerItem>(item =>
        {
            var fileName = CustomSoundsLibrary.GetFileName(item.SettingsValue);
            if (fileName == null) return;

            CustomSoundsLibrary.Delete(fileName);

            if (SelectedItem == item)
                SelectedItem = null;

            _allGroups = BuildGroups();
            ApplyFilter(SearchText);
            FilterChanged?.Invoke();
        });

        this.WhenAnyValue(x => x.SearchText)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(150))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(text =>
            {
                ApplyFilter(text);
                FilterChanged?.Invoke();
            })
            .DisposeWith(_disposables);
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

    public async Task PreviewItem(SoundPickerItem item)
    {
        _soundManager.StopSound();
        await Task.Delay(100).ConfigureAwait(false);

        var source = item.SettingsValue;
        if (string.IsNullOrEmpty(source)) return;

        // Built-in tones are short — preview with a cap. Other sounds play in full.
        bool isShortTone = BuiltInSounds.IsBuiltIn(source);
        int previewMs = isShortTone ? 5000 : 60_000;
        await _soundManager.PlaySoundAsync(source, loop: false, durationMs: previewMs).ConfigureAwait(false);
    }

    public void StopPreview()
    {
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

    public SoundPickerItem(string name, string? settingsValue)
    {
        Name = name;
        SettingsValue = settingsValue;
    }
}
