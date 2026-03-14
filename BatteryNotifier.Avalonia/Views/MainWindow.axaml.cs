using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly ILogger _logger;
    private const int TrayMargin = 8;
    private IDisposable? _aboutInteractionHandler;

    public MainWindow()
    {
        InitializeComponent();
        _logger = BatteryNotifierAppLogger.ForContext<MainWindow>();
    }

    protected override void OnPropertyChanged(global::Avalonia.AvaloniaPropertyChangedEventArgs change)
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

        _aboutInteractionHandler?.Dispose();
        _aboutInteractionHandler = null;

        if (DataContext is MainWindowViewModel vm)
        {
            _aboutInteractionHandler = vm.OpenAboutInteraction.RegisterHandler(async ctx =>
            {
                // Add backdrop overlay
                Panel? overlayHost = null;
                Control? existingContent = null;
                var backdrop = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
                    IsHitTestVisible = false
                };

                if (Content is Control content)
                {
                    existingContent = content;
                    overlayHost = new Panel();
                    Content = null;
                    overlayHost.Children.Add(existingContent);
                    overlayHost.Children.Add(backdrop);
                    Content = overlayHost;
                }

                try
                {
                    var aboutWindow = new AboutWindow();
                    await aboutWindow.ShowLightDismiss(this);
                }
                finally
                {
                    if (overlayHost != null && existingContent != null)
                    {
                        overlayHost.Children.Clear();
                        Content = existingContent;
                    }
                }

                ctx.SetOutput(System.Reactive.Unit.Default);
            });
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

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        PositionNearNotificationArea();
    }

    protected override void OnClosed(EventArgs e)
    {
        _aboutInteractionHandler?.Dispose();
        if (DataContext is MainWindowViewModel vm)
            vm.PropertyChanged -= (_, _) => { };
        base.OnClosed(e);
    }
}