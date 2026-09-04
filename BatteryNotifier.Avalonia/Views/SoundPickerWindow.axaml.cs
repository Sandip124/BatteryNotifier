using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BatteryNotifier.Avalonia.ViewModels;

namespace BatteryNotifier.Avalonia.Views;

public partial class SoundPickerWindow : Window
{
    private IDisposable? _selectSub;
    private IDisposable? _cancelSub;
    private IDisposable? _browseSub;

    private bool _suppressLightDismiss;
    private bool _isClosing;
    private TaskCompletionSource<SoundPickerItem?>? _tcs;
    
    private static readonly TimeSpan ShowSettleTime = TimeSpan.FromMilliseconds(450);
    private DateTime _suppressDismissUntil;

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
        _tcs = new TaskCompletionSource<SoundPickerItem?>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Position relative to owner center
        if (owner.Position.X > 0 || owner.Position.Y > 0)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            var x = owner.Position.X + (int)((owner.Width - Width) / 2);
            var y = owner.Position.Y + (int)((owner.Height - Height) / 2);
            Position = new global::Avalonia.PixelPoint(x, y);
        }

        _suppressDismissUntil = DateTime.UtcNow + ShowSettleTime;
        Show(owner);
        return _tcs.Task;
    }

    private void CloseWithResult(SoundPickerItem? result)
    {
        if (_isClosing) return;
        _isClosing = true;

        Deactivated -= OnWindowDeactivated;

        if (_tcs != null && !_tcs.Task.IsCompleted)
            _tcs.TrySetResult(result);
        Close();
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (_suppressLightDismiss || _isClosing) return;

        if (DateTime.UtcNow < _suppressDismissUntil)
        {
            global::Avalonia.Threading.DispatcherTimer.RunOnce(() =>
            {
                if (!_suppressLightDismiss && !_isClosing && !IsActive && _tcs is { Task.IsCompleted: false })
                    CloseWithResult(null);
            }, TimeSpan.FromMilliseconds(150));
            return;
        }

        CloseWithResult(null);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() => SearchBox?.Focus());
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

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (SearchBox is { IsFocused: false } box && !string.IsNullOrEmpty(e.Text))
        {
            box.Focus();
            box.Text = (box.Text ?? string.Empty) + e.Text;
            box.CaretIndex = box.Text.Length;
            e.Handled = true;
            return;
        }
        base.OnTextInput(e);
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
                _suppressLightDismiss = true;
                try
                {
                    var path = await BrowseAudioFile().ConfigureAwait(false);
                    ctx.SetOutput(path);
                }
                finally
                {
                    _suppressLightDismiss = false;
                }
            });
        }
    }

    // Row click selects only — it never plays (auto-play on click is annoying).
    // Selection is shown via the data-bound "Selected" badge (SoundPickerItem.IsSelected).
    private void OnSoundItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SoundPickerItem item }
            && DataContext is SoundPickerViewModel vm)
            vm.SelectedItem = item;
    }

    // Play/pause button auditions the sound and selects it, so hitting Select picks what you heard.
    private void OnPreviewToggleClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true; // handle here; don't also fire the row's click

        if (sender is Button { DataContext: SoundPickerItem item }
            && DataContext is SoundPickerViewModel vm)
        {
            vm.SelectedItem = item;
            vm.TogglePreview(item);
        }
    }

    private async void OnDeleteCustomClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true; 

        if (sender is not Button { DataContext: SoundPickerItem item }) return;
        if (DataContext is not SoundPickerViewModel vm) return;

        _suppressLightDismiss = true;
        bool confirmed;
        try
        {
            confirmed = await ConfirmDialog.ShowAsync(this,
                "Remove sound?",
                $"“{item.DisplayName}” will be removed from your library.",
                "Remove");
        }
        finally
        {
            _suppressLightDismiss = false;
        }

        if (confirmed)
            vm.DeleteCustomCommand.Execute(item).Subscribe();
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

        _tcs?.TrySetResult(null);

        _selectSub?.Dispose();
        _cancelSub?.Dispose();
        _browseSub?.Dispose();

        if (DataContext is SoundPickerViewModel vm)
            vm.Dispose();
    }
}
