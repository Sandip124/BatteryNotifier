using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace BatteryNotifier.Avalonia.Views;

public partial class ScreenFlashOverlay : Window
{
    private CancellationTokenSource? _flashCts;

    // CGWindowLevel constants (from CGWindowLevel.h)
    private const int NsWindowLevelScreenSaver = 1000;      // kCGScreenSaverWindowLevel
    internal const int NsWindowLevelAboveScreenSaver = 1001;  // screenSaver + 1 (for NotificationCard)

    public ScreenFlashOverlay()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (OperatingSystem.IsMacOS())
            ConfigureMacOverlay();
        else if (OperatingSystem.IsWindows())
            ConfigureWindowsOverlay();
    }

    public async Task FlashAsync(Color glowColor, int durationMs = Core.Constants.NotificationDurationMs)
    {
        _flashCts?.CancelAsync();
        _flashCts?.Dispose();
        _flashCts = new CancellationTokenSource();
        var ct = _flashCts.Token;

        GlowControl.GlowColor = glowColor;

        const double peakOpacity = 0.4;
        const int fadeInMs = 400;
        const int holdMs = 500;
        const int fadeOutMs = 600;
        const int pauseMs = 250;
        const int pulseMs = fadeInMs + holdMs + fadeOutMs + pauseMs;
        var pulseCount = Math.Max(1, durationMs / pulseMs);
        var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);

        for (int i = 0; i < pulseCount && DateTime.UtcNow < deadline && !ct.IsCancellationRequested; i++)
        {
            await CreateFadeAnimation(0.0, peakOpacity, fadeInMs).RunAsync(GlowControl, ct);
            await Task.Delay(holdMs, ct);
            await CreateFadeAnimation(peakOpacity, 0.0, fadeOutMs).RunAsync(GlowControl, ct);

            if (i < pulseCount - 1)
                await Task.Delay(pauseMs, ct);
        }

        Close();
    }

    public void StopFlash()
    {
        _flashCts?.Cancel();
        _flashCts?.Dispose();
        _flashCts = null;
        Close();
    }

    private static Animation CreateFadeAnimation(double from, double to, int durationMs) => new()
    {
        Duration = TimeSpan.FromMilliseconds(durationMs),
        FillMode = FillMode.Forward,
        Children =
        {
            new KeyFrame { Cue = new Cue(0), Setters = { new Setter(OpacityProperty, from) } },
            new KeyFrame { Cue = new Cue(1), Setters = { new Setter(OpacityProperty, to) } }
        }
    };

    // ── macOS: overlay above menu bar, Dock, and fullscreen apps ──

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

    [StructLayout(LayoutKind.Sequential)]
    private struct NSRect { public double X, Y, Width, Height; }

    // ARM64 returns structs in registers; x86_64 uses objc_msgSend_stret
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern NSRect objc_msgSend_NSRect(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
    private static extern void objc_msgSend_stret_NSRect(out NSRect result, IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_NSRect_Bool(IntPtr receiver, IntPtr selector, NSRect frame, bool display);

    private void ConfigureMacOverlay()
    {
        var nsWindow = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (nsWindow == IntPtr.Zero) return;

        // Window level 1000 — above Dock (20), menu bar (24), popups (101)
        objc_msgSend_IntPtr(nsWindow, sel_registerName("setLevel:"),
            NsWindowLevelScreenSaver);

        // Click-through
        objc_msgSend_Bool(nsWindow, sel_registerName("setIgnoresMouseEvents:"), true);

        // canJoinAllSpaces | stationary | ignoresCycle | fullScreenAuxiliary
        const long collectionBehavior = (1 << 0) | (1 << 4) | (1 << 6) | (1 << 8);
        objc_msgSend_IntPtr(nsWindow, sel_registerName("setCollectionBehavior:"),
            (IntPtr)collectionBehavior);

        // Exclude from screen capture (sharingType = .none = 0)
        objc_msgSend_IntPtr(nsWindow, sel_registerName("setSharingType:"), IntPtr.Zero);

        // Expand frame to full screen (including menu bar + Dock area).
        // macOS constrainFrameRect otherwise clips to visibleFrame.
        SetFrameToFullScreen(nsWindow);
    }

    private static void SetFrameToFullScreen(IntPtr nsWindow)
    {
        var mainScreen = objc_msgSend(objc_getClass("NSScreen"), sel_registerName("mainScreen"));
        if (mainScreen == IntPtr.Zero) return;

        var frameSel = sel_registerName("frame");
        var screenFrame = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
            ? objc_msgSend_NSRect(mainScreen, frameSel)
            : GetNSRect_x64(mainScreen, frameSel);

        objc_msgSend_NSRect_Bool(nsWindow, sel_registerName("setFrame:display:"), screenFrame, true);
    }

    private static NSRect GetNSRect_x64(IntPtr receiver, IntPtr selector)
    {
        objc_msgSend_stret_NSRect(out var rect, receiver, selector);
        return rect;
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
        var handle = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (handle == IntPtr.Zero) return;

        const int GWL_EXSTYLE = -20;
        const int WS_EX_TRANSPARENT = 0x20;
        const int WS_EX_LAYERED = 0x80000;

        int style = GetWindowLong(handle, GWL_EXSTYLE);
        SetWindowLong(handle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);

        // Exclude from screen capture (WDA_EXCLUDEFROMCAPTURE = 0x11)
        SetWindowDisplayAffinity(handle, 0x11);
    }
}