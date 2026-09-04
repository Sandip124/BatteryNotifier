using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Transformation;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.Services;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Core.Services;
using BatteryNotifier.Core.Utils;

namespace BatteryNotifier.Avalonia.Views;

public partial class MainWindow : Window
{
    private static readonly TransformOperations SettingsOffScreen = TransformOperations.Parse("translateX(400px)");
    private static readonly TransformOperations SettingsOnScreen = TransformOperations.Parse("translateX(0px)");
    private static readonly TimeSpan SettingsAnimDuration = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan SettingsOpacityDuration = TimeSpan.FromMilliseconds(150);

    private static Transitions MakeSettingsTransitions(Easing easing) => new()
    {
        new TransformOperationsTransition { Property = RenderTransformProperty, Duration = SettingsAnimDuration, Easing = easing },
        new DoubleTransition { Property = OpacityProperty, Duration = SettingsOpacityDuration, Easing = easing }
    };

    private readonly Debouncer _positionSaveDebouncer = new();
    private const int TrayMargin = 8;
    private INotifyPropertyChanged? _subscribedViewModel;
    private MainWindowViewModel? _subscribedMainVm;
    private bool _isSettingsAnimating;

    private const int AutoHideGraceMs = 150;
    private static readonly TimeSpan ShowSettleTime = TimeSpan.FromMilliseconds(500);
    private DateTime _suppressAutoHideUntil;
    private bool _autoHidePending;

    public MainWindow()
    {
        InitializeComponent();

        // Auto-hide like a taskbar/menu-bar flyout
        Deactivated += (_, _) => ScheduleAutoHideCheck();

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
        if (_subscribedMainVm != null)
        {
            _subscribedMainVm.SettingsCloseRequested -= AnimateSettingsClose;
            _subscribedMainVm = null;
        }

        if (DataContext is INotifyPropertyChanged npc)
        {
            npc.PropertyChanged += OnViewModelPropertyChanged;
            _subscribedViewModel = npc;
        }

        if (DataContext is MainWindowViewModel vm)
        {
            vm.SettingsCloseRequested += AnimateSettingsClose;
            _subscribedMainVm = vm;
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

    private void Logo_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        e.Handled = true; // don't also start a window drag via the title bar's handler
        TrayIconService.OpenAbout();
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

        // Only handle open — close is driven by SettingsCloseRequested event
        // so the content stays visible during the slide-out animation
        if (vm.CurrentView != null)
            AnimateSettingsOpen();
    }

    private void AnimateSettingsOpen()
    {
        if (_isSettingsAnimating) return;
        _isSettingsAnimating = true;

        SettingsContent.Transitions = MakeSettingsTransitions(new CubicEaseOut());
        SettingsContent.Opacity = 0;
        SettingsContent.RenderTransform = SettingsOffScreen;
        SettingsContent.IsVisible = true;

        DispatcherTimer.RunOnce(() =>
        {
            SettingsContent.Opacity = 1;
            SettingsContent.RenderTransform = SettingsOnScreen;
        }, TimeSpan.FromMilliseconds(16));

        DispatcherTimer.RunOnce(() => _isSettingsAnimating = false, SettingsAnimDuration);
    }

    private void AnimateSettingsClose()
    {
        if (_isSettingsAnimating) return;
        _isSettingsAnimating = true;

        SettingsContent.Transitions = MakeSettingsTransitions(new CubicEaseIn());
        SettingsContent.Opacity = 0;
        SettingsContent.RenderTransform = SettingsOffScreen;

        DispatcherTimer.RunOnce(() =>
        {
            SettingsContent.IsVisible = false;
            _isSettingsAnimating = false;
        }, SettingsAnimDuration);
    }

    // ── Flyout-style auto-hide ─────────────────────────────────────

    /// <summary>
    /// Schedules a deferred re-check of whether the window should auto-hide.
    /// </summary>
    public void ScheduleAutoHideCheck()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(ScheduleAutoHideCheck);
            return;
        }

        if (_autoHidePending) return;
        _autoHidePending = true;
        DispatcherTimer.RunOnce(EvaluateAutoHide, TimeSpan.FromMilliseconds(AutoHideGraceMs));
    }

    /// <summary>
    /// Call immediately after programmatically showing the window so the brief
    /// activation settle (a stray Deactivated during Show) doesn't hide it again.
    /// </summary>
    public void NotifyShown() => _suppressAutoHideUntil = DateTime.UtcNow + ShowSettleTime;

    private void EvaluateAutoHide()
    {
        _autoHidePending = false;

        if (!IsVisible || IsActive) return;                          
        if (DateTime.UtcNow < _suppressAutoHideUntil)                
        {
            ScheduleAutoHideCheck();
            return;
        }
        if (OwnedWindows.Count > 0) return;                          

        var appFocused = AppFocusTracker.IsApplicationFocused();
        if (appFocused == true) return;                             
        if (appFocused == null && AnyAppWindowActive()) return;    

        HideToTray();
    }

    private static bool AnyAppWindowActive()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return false;

        foreach (var w in desktop.Windows)
            if (w.IsActive) return true;
        return false;
    }

    /// <summary>
    /// Hides the window to the tray and restores efficiency mode
    /// </summary>
    public void HideToTray()
    {
        Hide();
        MacOSDockIconHelper.HideDockIcon();
        EfficiencyModeService.Instance.EnableEfficiency();
    }

    protected override void OnClosed(EventArgs e)
    {
        PositionChanged -= OnPositionChanged;
        _positionSaveDebouncer.Dispose();
        base.OnClosed(e);
    }
}