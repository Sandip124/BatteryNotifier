using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using BatteryNotifier.Avalonia.ViewModels;

namespace BatteryNotifier.Avalonia.Views;

public partial class SoundPickerWindow : Window
{
    private IDisposable? _selectSub;
    private IDisposable? _cancelSub;
    private IDisposable? _browseSub;

    private bool _closingFromBrowse;
    private TaskCompletionSource<SoundPickerItem?>? _tcs;

    public SoundPickerWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsLinux())
        {
            SystemDecorations = SystemDecorations.None;
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        }
        Deactivated += OnWindowDeactivated;
    }

    /// <summary>
    /// Shows the picker as a non-modal window (so Deactivated fires on owner click)
    /// but awaits a result like ShowDialog.
    /// </summary>
    public Task<SoundPickerItem?> ShowLightDismiss(Window owner)
    {
        _tcs = new TaskCompletionSource<SoundPickerItem?>();

        // Position relative to owner center
        if (owner.Position.X > 0 || owner.Position.Y > 0)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            var x = owner.Position.X + (int)((owner.Width - Width) / 2);
            var y = owner.Position.Y + (int)((owner.Height - Height) / 2);
            Position = new global::Avalonia.PixelPoint(x, y);
        }

        Show(owner);
        return _tcs.Task;
    }

    private void CloseWithResult(SoundPickerItem? result)
    {
        if (_tcs != null && !_tcs.Task.IsCompleted)
            _tcs.TrySetResult(result);
        Close();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (_closingFromBrowse) return;
        CloseWithResult(null);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CloseWithResult(null);
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        _selectSub?.Dispose();
        _cancelSub?.Dispose();
        _browseSub?.Dispose();

        if (DataContext is SoundPickerViewModel vm)
        {
            _selectSub = vm.SelectCommand.Subscribe(item =>
            {
                CloseWithResult(item);
            });

            _cancelSub = vm.CancelCommand.Subscribe(_ =>
            {
                CloseWithResult(null);
            });

            _browseSub = vm.BrowseFileInteraction.RegisterHandler(async ctx =>
            {
                _closingFromBrowse = true;
                try
                {
                    var path = await BrowseAudioFile().ConfigureAwait(false);
                    ctx.SetOutput(path);
                }
                finally
                {
                    _closingFromBrowse = false;
                }
            });

            vm.FilterChanged += () =>
                Dispatcher.UIThread.Post(() => UpdateCheckIcons(vm), DispatcherPriority.Render);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (DataContext is SoundPickerViewModel vm)
            Dispatcher.UIThread.Post(() => UpdateCheckIcons(vm), DispatcherPriority.Render);
    }

    private async void OnSoundItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not SoundPickerItem item)
            return;

        if (DataContext is not SoundPickerViewModel vm)
            return;

        vm.SelectedItem = item;
        UpdateCheckIcons(vm);

        await vm.PreviewItem(item).ConfigureAwait(false);
    }

    private void OnDeleteCustomClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true; // Prevent the parent button's OnSoundItemClick from firing

        if (sender is not Button button || button.DataContext is not SoundPickerItem item)
            return;

        if (DataContext is SoundPickerViewModel vm)
            vm.DeleteCustomCommand.Execute(item).Subscribe();
    }

    private void UpdateCheckIcons(SoundPickerViewModel vm)
    {
        var hoverBrush = this.TryFindResource("AppHoverBackground", ActualThemeVariant, out var res) && res is IBrush b
            ? b : Brushes.Transparent;

        foreach (var descendant in this.GetVisualDescendants())
        {
            if (descendant is not Button btn || btn.DataContext is not SoundPickerItem item)
                continue;

            var isSelected = vm.SelectedItem == item;
            btn.Background = isSelected ? hoverBrush : Brushes.Transparent;
            SetCheckIconVisibility(btn, isSelected);
        }
    }

    private static void SetCheckIconVisibility(Button btn, bool visible)
    {
        foreach (var child in btn.GetVisualDescendants())
        {
            if (child is PathIcon { Name: "CheckIcon" } icon)
                icon.IsVisible = visible;
        }
    }

    private async Task<string?> BrowseAudioFile()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Sound File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Audio Files")
                {
                    Patterns = ["*.wav", "*.mp3", "*.m4a", "*.wma", "*.ogg", "*.flac", "*.aac"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*.*"]
                }
            ]
        }).ConfigureAwait(false);

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Ensure TCS is resolved if window closed by other means
        _tcs?.TrySetResult(null);

        _selectSub?.Dispose();
        _cancelSub?.Dispose();
        _browseSub?.Dispose();

        if (DataContext is SoundPickerViewModel vm)
            vm.Dispose();
    }
}
