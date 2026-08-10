using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BatteryNotifier.Avalonia.Controls;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Models;

namespace BatteryNotifier.Avalonia.Views;

// _flashCts is disposed via the Window lifecycle (OnClosed + on every reuse/stop), not IDisposable.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "Window manages _flashCts through OnClosed and reuse, not via IDisposable.")]
public partial class ScreenFlashOverlay : Window
{
    private CancellationTokenSource? _flashCts;
    private bool _closing;
    private int _flashGeneration;

    private const int StopFadeOutMs = 250;

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

        try
        {
            if (OperatingSystem.IsMacOS())
                ConfigureMacOverlay();
            else if (OperatingSystem.IsWindows())
                ConfigureWindowsOverlay();
            else if (OperatingSystem.IsLinux())
                ConfigureLinuxOverlay();
        }
        catch (DllNotFoundException ex)
        {
            BatteryNotifierAppLogger.ForContext<ScreenFlashOverlay>()
                .Debug(ex, "Native overlay configuration unavailable — click-through disabled");
        }
    }

    private const double PeakOpacity = 0.4;
    // Intensity also drives the glow's band height: quiet → thin, loud → tall.
    private const double MinGlowThickness = 12;
    private const double MaxGlowThickness = 95;
    private const double DefaultGlowThickness = 60;
    // Approximate gap between sound-loop iterations (player re-spawn); mirrored in the flash loop.
    private const int LoopGapMs = 940;

    public async Task FlashAsync(Color glowColor,
        int durationMs = Core.Constants.NotificationDurationMs,
        FlashSequence? sequence = null)
    {
        // Cancel synchronously on the UI thread — animation teardown must not run off-thread.
        _flashCts?.Cancel();
        _flashCts?.Dispose();
        _flashCts = new CancellationTokenSource();
        var ct = _flashCts.Token;

        _closing = false; // reused from the pool — clear any prior stop state
        var generation = ++_flashGeneration;
        GlowControl.GlowColor = glowColor;

        try
        {
            if (sequence is { Intensities.Count: > 1 })
                await PlaySequenceAsync(sequence, durationMs, ct);
            else
                await PlayDefaultPulseAsync(durationMs, ct);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a new flash or a stop — whoever cancelled us handles hiding.
        }
        catch (Exception ex)
        {
            // FlashAsync is fire-and-forget; never let an exception go unobserved.
            BatteryNotifierAppLogger.ForContext<ScreenFlashOverlay>().Warning(ex, "Flash playback failed");
        }

        // Fade out gracefully
        if (!_closing && !ct.IsCancellationRequested)
            await FadeOutAndHideAsync(generation);
    }

    /// <summary>
    /// Drives the glow along a sound-derived intensity envelope, looped to fill the duration.
    /// Avalonia interpolates between the keyframes, so the sampled sequence plays back smoothly.
    /// </summary>
    private async Task PlaySequenceAsync(FlashSequence sequence, int durationMs, CancellationToken ct)
    {
        // Mirror the sound's per-loop restart gap only when the envelope is shorter than the window
        // and actually loops (built-ins span the window and play once).
        bool willLoop = sequence.DurationMs < durationMs;
        int gapMs = willLoop ? LoopGapMs : 0;
        int loopMs = sequence.DurationMs + gapMs;
        double playFraction = (double)sequence.DurationMs / loopMs;

        // Avalonia forbids RunAsync on an infinite animation; the timeout below trims to durationMs.
        var iterations = (ulong)Math.Max(1, (int)Math.Ceiling((double)durationMs / loopMs));

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(loopMs),
            IterationCount = new IterationCount(iterations),
            FillMode = FillMode.Forward
        };

        var count = sequence.Intensities.Count;
        for (int i = 0; i < count; i++)
        {
            var intensity = sequence.Intensities[i];
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(playFraction * ((double)i / (count - 1))),
                Setters =
                {
                    new Setter(OpacityProperty, intensity * PeakOpacity),
                    new Setter(EdgeGlowRenderer.GlowThicknessProperty,
                        MinGlowThickness + intensity * (MaxGlowThickness - MinGlowThickness)),
                }
            });
        }

        // Dark tail across the loop gap, mirroring the sound's silent restart pause (looping only).
        if (gapMs > 0)
        {
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1.0),
                Setters =
                {
                    new Setter(OpacityProperty, 0.0),
                    new Setter(EdgeGlowRenderer.GlowThicknessProperty, MinGlowThickness),
                }
            });
        }

        // Bound to durationMs. Cancel via DispatcherTimer (UI thread) — a threadpool timer
        // (CancelAfter) would tear down the animation off-thread and crash.
        using var window = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var timeout = DispatcherTimer.RunOnce(window.Cancel, TimeSpan.FromMilliseconds(durationMs));
        await animation.RunAsync(GlowControl, window.Token);
    }

    /// <summary>Fallback when a sound has no envelope yet: the original steady pulse.</summary>
    private async Task PlayDefaultPulseAsync(int durationMs, CancellationToken ct)
    {
        GlowControl.GlowThickness = DefaultGlowThickness; // reset in case a prior sequence left it small

        const int fadeInMs = 400;
        const int holdMs = 500;
        const int fadeOutMs = 600;
        const int pauseMs = 250;
        const int pulseMs = fadeInMs + holdMs + fadeOutMs + pauseMs;
        var pulseCount = Math.Max(1, durationMs / pulseMs);
        var deadline = DateTime.UtcNow.AddMilliseconds(durationMs);

        for (int i = 0; i < pulseCount && DateTime.UtcNow < deadline && !ct.IsCancellationRequested; i++)
        {
            await CreateFadeAnimation(0.0, PeakOpacity, fadeInMs).RunAsync(GlowControl, ct);
            await Task.Delay(holdMs, ct);
            await CreateFadeAnimation(PeakOpacity, 0.0, fadeOutMs).RunAsync(GlowControl, ct);

            if (i < pulseCount - 1)
                await Task.Delay(pauseMs, ct);
        }
    }

    /// <summary>
    /// Stops the flash gracefully and hides the overlay (kept alive for reuse from the pool).
    /// </summary>
    public void StopFlash()
    {
        if (_closing) return;
        _closing = true;

        _flashCts?.Cancel();
        _flashCts?.Dispose();
        _flashCts = null;

        _ = FadeOutAndHideAsync(_flashGeneration);
    }

    private async Task FadeOutAndHideAsync(int generation)
    {
        try
        {
            var from = GlowControl.Opacity;
            if (from > 0.001)
                await CreateFadeAnimation(from, 0.0, StopFadeOutMs).RunAsync(GlowControl);
        }
        catch (Exception ex)
        {
            BatteryNotifierAppLogger.ForContext<ScreenFlashOverlay>()
                .Debug(ex, "Screen flash fade-out failed");
        }

        if (generation == _flashGeneration)
            ResetHidden();
    }

    /// <summary>Returns the overlay to its idle hidden state without destroying it.</summary>
    private void ResetHidden()
    {
        GlowControl.Opacity = 0;
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        _flashCts?.Cancel();
        _flashCts?.Dispose();
        _flashCts = null;
        base.OnClosed(e);
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
            unchecked((IntPtr)collectionBehavior)); // small bitmask — cannot overflow

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

    // ── Linux: click-through via X11 XShape extension ──

    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libXext.so.6")]
    private static extern void XShapeCombineRectangles(
        IntPtr display, IntPtr window, int destKind, int xOff, int yOff,
        IntPtr rectangles, int nRects, int op, int ordering);

    private const int ShapeInput = 2;   // ShapeInput — input region (click-through)
    private const int ShapeSet = 0;     // ShapeSet — replace region

    private void ConfigureLinuxOverlay()
    {
        var xWindow = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (xWindow == IntPtr.Zero) return;

        var display = XOpenDisplay(IntPtr.Zero);
        if (display == IntPtr.Zero) return;

        try
        {
            // Set an empty input shape — all mouse events pass through
            XShapeCombineRectangles(display, xWindow, ShapeInput,
                0, 0, IntPtr.Zero, 0, ShapeSet, 0);
        }
        finally
        {
            XCloseDisplay(display);
        }
    }
}