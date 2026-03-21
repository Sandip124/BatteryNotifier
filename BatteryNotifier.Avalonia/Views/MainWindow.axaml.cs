using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Utils;

namespace BatteryNotifier.Avalonia.Views;

public partial class MainWindow : Window
{
    private static readonly TransformOperations SettingsOffScreen = TransformOperations.Parse("translateX(400px)");
    private static readonly TransformOperations SettingsOnScreen = TransformOperations.Parse("translateX(0px)");
    private static readonly TimeSpan SettingsAnimDuration = TimeSpan.FromMilliseconds(300);

    private readonly Debouncer _positionSaveDebouncer = new();
    private const int TrayMargin = 8;
    private INotifyPropertyChanged? _subscribedViewModel;
    private bool _isSettingsAnimating;

    public MainWindow()
    {
        InitializeComponent();

        // Linux WMs (GNOME, KDE) ignore ExtendClientAreaToDecorationsHint and draw
        // their own title bar with min/max/close buttons. Remove decorations entirely
        // on Linux so the app renders the same chromeless look as Windows/macOS.
        // X11 does not support AcrylicBlur — use Transparent so rounded corners show.
        if (OperatingSystem.IsLinux())
        {
            SystemDecorations = SystemDecorations.None;
            TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsVisibleProperty && DataContext is MainWindowViewModel vm)
        {
            vm.OnWindowVisibilityChanged(IsVisible);
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Unsubscribe from previous DataContext to prevent leak
        if (_subscribedViewModel != null)
        {
            _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _subscribedViewModel = null;
        }

        if (DataContext is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += OnViewModelPropertyChanged;
            _subscribedViewModel = npc;
        }
    }

    /// <summary>
    /// Positions the window near the platform's notification area.
    /// macOS: top-right (below menu bar). Windows/Linux: bottom-right (above taskbar).
    /// </summary>
    public void PositionNearNotificationArea()
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen == null) return;

        var scaling = screen.Scaling;
        var workArea = screen.WorkingArea;

        var winWidth = (int)(Width * scaling);
        var winHeight = (int)(Height * scaling);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: menu bar is at the top, tray icons are top-right
            Position = new PixelPoint(
                workArea.Right - winWidth - TrayMargin,
                workArea.Y + TrayMargin);
        }
        else
        {
            // Windows / Linux: taskbar is typically at the bottom
            Position = new PixelPoint(
                workArea.Right - winWidth - TrayMargin,
                workArea.Bottom - winHeight - TrayMargin);
        }
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void HealthBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.IsHealthSheetOpen = !vm.IsHealthSheetOpen;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var settings = AppSettings.Instance;
        if (settings.WindowPositionX.HasValue && settings.WindowPositionY.HasValue)
        {
            var saved = new PixelPoint(settings.WindowPositionX.Value, settings.WindowPositionY.Value);

            // Validate the saved position is still on a visible screen
            var isOnScreen = Screens.All.Any(screen => screen.WorkingArea.Contains(saved));

            if (isOnScreen)
                Position = saved;
            else
                PositionNearNotificationArea();
        }
        else
        {
            PositionNearNotificationArea();
        }

        PositionChanged += OnPositionChanged;
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        _positionSaveDebouncer.Debounce(() =>
        {
            var settings = AppSettings.Instance;
            settings.WindowPositionX = e.Point.X;
            settings.WindowPositionY = e.Point.Y;
            settings.Save();
        }, 500);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.CurrentView)) return;
        if (sender is not MainWindowViewModel vm) return;

        if (vm.CurrentView != null)
            AnimateSettingsOpen();
        else
            AnimateSettingsClose();
    }

    private void AnimateSettingsOpen()
    {
        if (_isSettingsAnimating) return;

        SettingsContent.Opacity = 0;
        SettingsContent.RenderTransform = SettingsOffScreen;
        SettingsContent.IsVisible = true;

        DispatcherTimer.RunOnce(() =>
        {
            SettingsContent.Opacity = 1;
            SettingsContent.RenderTransform = SettingsOnScreen;
        }, TimeSpan.FromMilliseconds(16));
    }

    private void AnimateSettingsClose()
    {
        if (_isSettingsAnimating) return;
        _isSettingsAnimating = true;

        SettingsContent.Opacity = 0;
        SettingsContent.RenderTransform = SettingsOffScreen;

        DispatcherTimer.RunOnce(() =>
        {
            SettingsContent.IsVisible = false;
            _isSettingsAnimating = false;
        }, SettingsAnimDuration);
    }

    protected override void OnClosed(EventArgs e)
    {
        PositionChanged -= OnPositionChanged;
        _positionSaveDebouncer.Dispose();
        base.OnClosed(e);
    }
}