using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace BatteryNotifier.Avalonia.Views;

public partial class ScreenFlashOverlay : Window
{
    private CancellationTokenSource? _flashCts;

    public ScreenFlashOverlay()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ConfigureNativeOverlay();
    }

    public async Task FlashAsync(Color glowColor, int durationMs = Core.Constants.NotificationDurationMs)
    {
        _flashCts = new CancellationTokenSource();
        var ct = _flashCts.Token;

        GlowControl.GlowColor = glowColor;

        // Each pulse: 200ms fade-in + 400ms hold + 400ms fade-out + 150ms pause = ~1150ms
        const int pulseMs = 1150;
        var pulseCount = Math.Max(1, durationMs / pulseMs);
        var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);

        try
        {
            for (int i = 0; i < pulseCount && DateTime.UtcNow < deadline && !ct.IsCancellationRequested; i++)
            {
                var fadeIn = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(200),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 0.0) } },
                        new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 0.85) } }
                    }
                };
                await fadeIn.RunAsync(GlowControl, ct);

                await Task.Delay(400, ct);

                var fadeOut = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(400),
                    FillMode = FillMode.Forward,
                    Children =
                    {
                        new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, 0.85) } },
                        new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, 0.0) } }
                    }
                };
                await fadeOut.RunAsync(GlowControl, ct);

                if (i < pulseCount - 1)
                    await Task.Delay(150, ct);
            }
        }
        catch (OperationCanceledException) { }

        try { Close(); } catch { }
    }

    public void StopFlash()
    {
        _flashCts?.Cancel();
        try { Close(); } catch { }
    }

    private void ConfigureNativeOverlay()
    {
        if (OperatingSystem.IsMacOS())
            ConfigureMacOverlay();
        else if (OperatingSystem.IsWindows())
            ConfigureWindowsOverlay();
    }

    // ── macOS: click-through + above menu bar ──

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_getClass(string className);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_Bool(IntPtr receiver, IntPtr selector, bool arg);

    // NSWindow level constants
    internal const int NSWindowLevelScreenSaver = 101;
    internal const int NSWindowLevelAboveScreenSaver = 102;

    private void ConfigureMacOverlay()
    {
        try
        {
            // TryGetPlatformHandle().Handle on macOS is already the NSWindow (AvnWindow subclass)
            var nsWindow = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (nsWindow == IntPtr.Zero) return;

            // Level 101 (screenSaver) — above full-screen apps, menu bar, dock
            objc_msgSend_IntPtr(nsWindow, sel_registerName("setLevel:"),
                (IntPtr)NSWindowLevelScreenSaver);

            // Click-through — mouse events pass to apps beneath
            objc_msgSend_Bool(nsWindow, sel_registerName("setIgnoresMouseEvents:"), true);

            // Visible on all Spaces, stationary (doesn't move with Space switch), ignored by Cmd+Tab
            // canJoinAllSpaces=1<<0, stationary=1<<4, ignoresCycle=1<<6
            const long collectionBehavior = (1 << 0) | (1 << 4) | (1 << 6);
            objc_msgSend_IntPtr(nsWindow, sel_registerName("setCollectionBehavior:"),
                (IntPtr)collectionBehavior);

            // Exclude from screen capture/recording (sharingType = .none = 0)
            objc_msgSend_IntPtr(nsWindow, sel_registerName("setSharingType:"), IntPtr.Zero);
        }
        catch { /* best effort */ }
    }

    // ── Windows: click-through via extended window style ──

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    private void ConfigureWindowsOverlay()
    {
        try
        {
            var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;

            const int GWL_EXSTYLE = -20;
            const int WS_EX_TRANSPARENT = 0x20;
            const int WS_EX_LAYERED = 0x80000;

            // Click-through
            int style = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            // Exclude from screen capture (WDA_EXCLUDEFROMCAPTURE = 0x11)
            SetWindowDisplayAffinity(handle, 0x11);
        }
        catch { /* best effort */ }
    }
}