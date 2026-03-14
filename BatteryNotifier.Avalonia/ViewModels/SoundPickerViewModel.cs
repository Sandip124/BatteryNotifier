using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using BatteryNotifier.Core.Managers;
using ReactiveUI;
using Serilog;

namespace BatteryNotifier.Avalonia.ViewModels;

public class SoundPickerViewModel : ViewModelBase, IDisposable
{
    private readonly SoundManager _soundManager = new();
    private readonly CompositeDisposable _disposables = new();
    private readonly List<SoundPickerGroup> _allGroups;
    private readonly SoundPickerItem? _customItem;

    private SoundPickerItem? _selectedItem;
    private string? _searchText;
    private List<SoundPickerGroup> _filteredGroups = [];
    private bool _disposed;

    public string PickerTitle { get; }
    public ReactiveCommand<Unit, SoundPickerItem?> SelectCommand { get; }
    public ReactiveCommand<Unit, SoundPickerItem?> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseCustomCommand { get; }

    public Interaction<Unit, string?> BrowseFileInteraction { get; } = new();
    public event Action? FilterChanged;

    public SoundPickerItem? SelectedItem
    {
        get => _selectedItem;
        set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set => this.RaiseAndSetIfChanged(ref _searchText, value);
    }

    public List<SoundPickerGroup> FilteredGroups
    {
        get => _filteredGroups;
        private set => this.RaiseAndSetIfChanged(ref _filteredGroups, value);
    }

    public SoundPickerViewModel(string? currentSettingsValue, string sectionTitle)
    {
        PickerTitle = $"Choose {sectionTitle} Sound";
        _allGroups = BuildGroups();

        // Track custom item separately — only shown in list when it has a path
        _customItem = new SoundPickerItem("Custom file", null) { IsCustom = true };

        // Set initial selection
        if (!string.IsNullOrEmpty(currentSettingsValue))
        {
            if (BuiltInSounds.IsBuiltIn(currentSettingsValue))
            {
                var name = BuiltInSounds.GetName(currentSettingsValue);
                SelectedItem = _allGroups.SelectMany(g => g.Items).FirstOrDefault(i => i.Name == name);
            }
            else
            {
                _customItem.CustomPath = currentSettingsValue;
                SelectedItem = _customItem;
            }
        }

        ApplyFilter(null);

        var canSelect = this.WhenAnyValue(x => x.SelectedItem)
            .Select(item => item != null);
        SelectCommand = ReactiveCommand.Create(() => SelectedItem, canSelect);
        CancelCommand = ReactiveCommand.Create(() => (SoundPickerItem?)null);

        BrowseCustomCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            var path = await BrowseFileInteraction.Handle(Unit.Default);
            if (!string.IsNullOrEmpty(path))
            {
                _customItem!.CustomPath = path;
                SelectedItem = _customItem;
                ApplyFilter(SearchText);
            }
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
                ? group.Items.Where(i => i.Name.Contains(search!, StringComparison.OrdinalIgnoreCase)).ToList()
                : group.Items.ToList();

            if (items.Count > 0)
                filtered.Add(new SoundPickerGroup(group.Title, items));
        }

        // Show custom item in its own group only if it has a path
        if (_customItem != null && !string.IsNullOrEmpty(_customItem.CustomPath))
        {
            var customName = _customItem.DisplayName;
            if (!hasSearch || customName.Contains(search!, StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(new SoundPickerGroup("Custom", [_customItem]));
            }
        }

        FilteredGroups = filtered;
    }

    public async Task PreviewItem(SoundPickerItem item)
    {
        try
        {
            _soundManager.StopSound();
            await Task.Delay(100);

            string? source = item.IsCustom ? item.CustomPath : item.SettingsValue;
            if (string.IsNullOrEmpty(source)) return;

            await _soundManager.PlaySoundAsync(source, loop: false, durationMs: 5000);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Sound preview failed for {Name}", item.Name);
        }
    }

    public void StopPreview()
    {
        try { _soundManager.StopSound(); }
        catch { /* best effort */ }
    }

    private static List<SoundPickerGroup> BuildGroups()
    {
        return
        [
            new SoundPickerGroup("Full Battery — Calm",
            [
                new("Zen", BuiltInSounds.ToSettingsValue("Zen")),
                new("Harp", BuiltInSounds.ToSettingsValue("Harp")),
                new("Breeze", BuiltInSounds.ToSettingsValue("Breeze")),
                new("Bloom", BuiltInSounds.ToSettingsValue("Bloom")),
            ]),
            new SoundPickerGroup("Low Battery — Warning",
            [
                new("Pulse", BuiltInSounds.ToSettingsValue("Pulse")),
                new("Klaxon", BuiltInSounds.ToSettingsValue("Klaxon")),
                new("Rattle", BuiltInSounds.ToSettingsValue("Rattle")),
            ]),
            new SoundPickerGroup("General",
            [
                new("Chime", BuiltInSounds.ToSettingsValue("Chime")),
                new("Alert", BuiltInSounds.ToSettingsValue("Alert")),
                new("Beacon", BuiltInSounds.ToSettingsValue("Beacon")),
            ]),
        ];
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

public class SoundPickerGroup(string title, List<SoundPickerItem> items)
{
    public string Title { get; } = title;
    public List<SoundPickerItem> Items { get; } = items;
}

public class SoundPickerItem : ReactiveObject
{
    private string? _customPath;

    public string Name { get; }
    public string? SettingsValue { get; }
    public bool IsCustom { get; init; }

    public string? CustomPath
    {
        get => _customPath;
        set
        {
            this.RaiseAndSetIfChanged(ref _customPath, value);
            this.RaisePropertyChanged(nameof(DisplayName));
        }
    }

    public string DisplayName => IsCustom && !string.IsNullOrEmpty(CustomPath)
        ? System.IO.Path.GetFileName(CustomPath) ?? "Custom file"
        : Name;

    public SoundPickerItem(string name, string? settingsValue)
    {
        Name = name;
        SettingsValue = settingsValue;
    }
}
