using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.ViewModels;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Avalonia.Views;

public partial class NotificationCard : Window
{
    // Entrance/exit animation tuning.
    private const double HiddenScale = 0.9;          // shrink factor for the hidden state
    private const int SlideOffsetPx = 12;            // distance the card slides toward its edge

    private static readonly TimeSpan AnimOutDuration = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan AnimStartDelay = TimeSpan.FromMilliseconds(16);   // ~1 frame, lets transitions apply
    private static readonly TimeSpan ProgressTickInterval = TimeSpan.FromMilliseconds(30);

    private static readonly TransformOperations Visible = TransformOperations.Parse("scale(1,1) translate(0px,0px)");
    // Hidden state emanates from the docked corner; recomputed per position by SetAnchor.
    private TransformOperations _hidden = BuildHiddenTransform(0, -SlideOffsetPx);

    private DispatcherTimer? _progressTimer;
    private DateTime _showTime;
    private bool _isDismissing;

    public NotificationCard()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Aligns the entrance/exit animation with the on-screen notification position so the card
    /// grows and slides from the corner/edge it's docked to (not always the top). Call before Show().
    /// </summary>
    public void SetAnchor(NotificationPosition position)
    {
        var (originX, originY) = AnchorOrigin(position);

        _hidden = BuildHiddenTransform(SlideForOrigin(originX), SlideForOrigin(originY));
        CardBorder.RenderTransformOrigin = new RelativePoint(originX, originY, RelativeUnit.Relative);
        CardBorder.RenderTransform = _hidden;
    }

    /// <summary>Origin at the docked corner: X ∈ {0 left, 0.5 center, 1 right}, Y ∈ {0 top, 1 bottom}.</summary>
    private static (double X, double Y) AnchorOrigin(NotificationPosition position) => position switch
    {
        NotificationPosition.TopLeft => (0, 0),
        NotificationPosition.TopCenter => (0.5, 0),
        NotificationPosition.TopRight => (1, 0),
        NotificationPosition.BottomLeft => (0, 1),
        NotificationPosition.BottomCenter => (0.5, 1),
        NotificationPosition.BottomRight => (1, 1),
        _ => (0.5, 0),
    };

    /// <summary>Slide the hidden state toward the anchored edge: origin 0 → -N, 0.5 → 0, 1 → +N.</summary>
    private static int SlideForOrigin(double origin) => (int)((origin - 0.5) * 2 * SlideOffsetPx);

    private static TransformOperations BuildHiddenTransform(int offsetX, int offsetY) =>
        TransformOperations.Parse(FormattableString.Invariant(
            $"scale({HiddenScale},{HiddenScale}) translate({offsetX}px,{offsetY}px)"));

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SetAboveFlashOverlay();
        ApplyAccentWash();

        // CardBorder starts hidden (opacity 0, scaled, offset — set in XAML / SetAnchor).
        // Flipping to the visible state one frame later triggers the transitions.
        DispatcherTimer.RunOnce(() =>
        {
            CardBorder.Opacity = 1;
            CardBorder.RenderTransform = Visible;
            StartCountdown();
        }, AnimStartDelay);
    }

    private void StartCountdown()
    {
        _showTime = DateTime.UtcNow;
        var duration = Core.Constants.NotificationDurationMs;

        ProgressBar.Width = CardBorder.Bounds.Width;

        _progressTimer = new DispatcherTimer { Interval = ProgressTickInterval };
        _progressTimer.Tick += (_, _) =>
        {
            var elapsed = (DateTime.UtcNow - _showTime).TotalMilliseconds;
            var remaining = Math.Max(0, 1.0 - elapsed / duration);

            ProgressBar.Width = CardBorder.Bounds.Width * remaining;

            if (remaining <= 0)
            {
                _progressTimer?.Stop();
                Dismiss(userInitiated: false);
            }
        };
        _progressTimer.Start();
    }

    private async Task Dismiss(bool userInitiated)
    {
        if (_isDismissing) return;
        _isDismissing = true;

        _progressTimer?.Stop();

        // Animate out toward the docked corner (mirrors the entrance)
        CardBorder.Opacity = 0;
        CardBorder.RenderTransform = _hidden;

        // Wait for the transition to finish
        await Task.Delay(AnimOutDuration).ConfigureAwait(true);

        if (DataContext is NotificationCardViewModel vm)
            vm.Dismiss(userInitiated);
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
        global::Avalonia.Interactivity.RoutedEventArgs e) => Dismiss(userInitiated: true);

    private void SetAboveFlashOverlay()
    {
        if (!OperatingSystem.IsMacOS()) return;
        try
        {
            var nsWindow = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (nsWindow == IntPtr.Zero) return;

            objc_msgSend_IntPtr(nsWindow, sel_registerName("setLevel:"),
                (IntPtr)ScreenFlashOverlay.NsWindowLevelAboveScreenSaver);
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
