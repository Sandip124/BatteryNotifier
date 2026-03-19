using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.ViewModels;

namespace BatteryNotifier.Avalonia.Views;

public partial class NotificationCard : Window
{
    private static readonly TransformOperations Hidden = TransformOperations.Parse("scale(0.9,0.9) translateY(-12px)");
    private static readonly TransformOperations Visible = TransformOperations.Parse("scale(1,1) translateY(0px)");
    private static readonly TimeSpan AnimInDuration = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan AnimOutDuration = TimeSpan.FromMilliseconds(300);

    private DispatcherTimer? _progressTimer;
    private DateTime _showTime;
    private bool _isDismissing;

    public NotificationCard()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SetAboveFlashOverlay();
        ApplyAccentWash();

        // CardBorder starts at opacity=0, scale=0.85 (set in XAML).
        // Transition properties trigger the animation on next frame.
        DispatcherTimer.RunOnce(() =>
        {
            CardBorder.Opacity = 1;
            CardBorder.RenderTransform = Visible;
            StartCountdown();
        }, TimeSpan.FromMilliseconds(16));
    }

    private void StartCountdown()
    {
        _showTime = DateTime.UtcNow;
        var duration = Core.Constants.NotificationDurationMs;

        ProgressBar.Width = CardBorder.Bounds.Width;

        _progressTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _progressTimer.Tick += (_, _) =>
        {
            var elapsed = (DateTime.UtcNow - _showTime).TotalMilliseconds;
            var remaining = Math.Max(0, 1.0 - elapsed / duration);

            ProgressBar.Width = CardBorder.Bounds.Width * remaining;

            if (remaining <= 0)
            {
                _progressTimer?.Stop();
                Dismiss();
            }
        };
        _progressTimer.Start();
    }

    public async void Dismiss()
    {
        if (_isDismissing) return;
        _isDismissing = true;

        _progressTimer?.Stop();

        // Animate out using the same transitions defined in XAML
        CardBorder.Opacity = 0;
        CardBorder.RenderTransform = Hidden;

        // Wait for the transition to finish
        await Task.Delay(AnimOutDuration);

        if (DataContext is NotificationCardViewModel vm)
            vm.DismissCommand.Execute().Subscribe();
    }

    private void ApplyAccentWash()
    {
        if (DataContext is not NotificationCardViewModel vm) return;

        AccentWash.Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(vm.AccentColorValue, 0.0),
                new GradientStop(Color.FromArgb(0, vm.AccentColorValue.R,
                    vm.AccentColorValue.G, vm.AccentColorValue.B), 1.0),
            }
        };
    }

    private void DismissButton_Click(object? sender,
        global::Avalonia.Interactivity.RoutedEventArgs e) => Dismiss();

    private void SetAboveFlashOverlay()
    {
        if (!OperatingSystem.IsMacOS()) return;
        try
        {
            var nsWindow = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (nsWindow == IntPtr.Zero) return;

            objc_msgSend_IntPtr(nsWindow, sel_registerName("setLevel:"),
                (IntPtr)ScreenFlashOverlay.NSWindowLevelAboveScreenSaver);
        }
        catch { /* best effort */ }
    }

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    protected override void OnClosed(EventArgs e)
    {
        _progressTimer?.Stop();
        _progressTimer = null;
        base.OnClosed(e);
    }
}
