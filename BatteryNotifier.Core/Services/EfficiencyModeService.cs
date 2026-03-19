using System.Runtime.InteropServices;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Manages OS-level power efficiency hints for the app.
/// - Windows: EcoQoS via SetProcessInformation (efficiency cores + lower frequency)
/// - macOS: App Nap prevention via NSProcessInfo activity assertion (keeps timers on time)
/// - Linux: nice value for CPU scheduling priority
///
/// Enable when the window is hidden (tray-only). Disable when visible or during notifications.
/// </summary>
public sealed class EfficiencyModeService : IDisposable
{
    private static readonly Lazy<EfficiencyModeService> _instance = new(() => new EfficiencyModeService());
    public static EfficiencyModeService Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("EfficiencyModeService");

    private readonly object _lock = new();
    private bool _disposed;
    private bool _isEfficient;

    /// <summary>Number of active "normal mode" requests. Efficiency mode only activates when zero.</summary>
    private int _normalModeRefCount;

    // macOS: retained activity assertion — prevents App Nap from throttling timers.
    // Must be prevented from GC via CFRetain (raw IntPtr is not tracked by .NET GC).
    private IntPtr _macActivityAssertion = IntPtr.Zero;

    private EfficiencyModeService()
    {
        // macOS: immediately claim an activity assertion to prevent App Nap
        // from throttling our 1s battery polling timer. Held for app lifetime.
        if (OperatingSystem.IsMacOS())
            AcquireMacActivityAssertion();
    }

    /// <summary>
    /// Enter efficiency mode (window hidden, idle). On Windows, hints the OS
    /// to schedule on efficiency cores at lower frequency.
    /// </summary>
    public void EnableEfficiency()
    {
        lock (_lock)
        {
            if (_disposed || _isEfficient) return;
            if (_normalModeRefCount > 0) return;
            _isEfficient = true;

            ApplyPlatformEfficiency(true);
        }

        Logger.Debug("Efficiency mode enabled");
    }

    /// <summary>
    /// Exit efficiency mode (window visible, notification active).
    /// </summary>
    public void DisableEfficiency()
    {
        lock (_lock)
        {
            if (_disposed || !_isEfficient) return;
            _isEfficient = false;

            ApplyPlatformEfficiency(false);
        }

        Logger.Debug("Efficiency mode disabled");
    }

    /// <summary>
    /// Acquire a "normal mode" hold. Efficiency mode won't activate while any holds exist.
    /// Call <see cref="ReleaseNormalMode"/> when done. Use for notifications, sound playback, etc.
    /// </summary>
    public void AcquireNormalMode()
    {
        lock (_lock)
        {
            _normalModeRefCount++;
            if (_isEfficient)
            {
                _isEfficient = false;
                ApplyPlatformEfficiency(false);
            }
        }
    }

    /// <summary>
    /// Release a "normal mode" hold. When all holds are released, efficiency mode
    /// can be re-enabled by calling <see cref="EnableEfficiency"/>.
    /// </summary>
    public void ReleaseNormalMode()
    {
        lock (_lock)
        {
            if (_normalModeRefCount > 0)
                _normalModeRefCount--;
        }
    }

    /// <summary>Must be called inside _lock.</summary>
    private static void ApplyPlatformEfficiency(bool enable)
    {
        if (OperatingSystem.IsWindows())
            SetWindowsEcoQoS(enable);
        else if (OperatingSystem.IsLinux())
            SetLinuxNice(enable);
    }

    // ── Windows: EcoQoS via SetProcessInformation ──

#if WINDOWS
    private const int ProcessPowerThrottling = 4;
    private const uint PROCESS_POWER_THROTTLING_CURRENT_VERSION = 1;
    private const uint PROCESS_POWER_THROTTLING_EXECUTION_SPEED = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_POWER_THROTTLING_STATE
    {
        public uint Version;
        public uint ControlMask;
        public uint StateMask;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessInformation(
        IntPtr hProcess, int ProcessInformationClass,
        ref PROCESS_POWER_THROTTLING_STATE processInformation,
        uint processInformationSize);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();
#endif

    private static void SetWindowsEcoQoS(bool enable)
    {
#if WINDOWS
        try
        {
            var state = new PROCESS_POWER_THROTTLING_STATE
            {
                Version = PROCESS_POWER_THROTTLING_CURRENT_VERSION,
                ControlMask = PROCESS_POWER_THROTTLING_EXECUTION_SPEED,
                StateMask = enable ? PROCESS_POWER_THROTTLING_EXECUTION_SPEED : 0
            };

            SetProcessInformation(GetCurrentProcess(), ProcessPowerThrottling,
                ref state, (uint)Marshal.SizeOf(state));
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to set Windows EcoQoS");
        }
#endif
    }

    // ── macOS: App Nap prevention via NSProcessInfo activity assertion ──

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_getClass(string className);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_ulong_IntPtr(
        IntPtr receiver, IntPtr selector, ulong options, IntPtr reason);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    // CFRetain/CFRelease to prevent ObjC object from being collected
    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFRetain(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string cStr, uint encoding);

    private const uint kCFStringEncodingUTF8 = 0x08000100;

    // NSActivityUserInitiatedAllowingIdleSystemSleep = 0x00EFFFFF
    // Prevents App Nap timer throttling while allowing system sleep
    private const ulong NSActivityUserInitiatedAllowingIdleSystemSleep = 0x00EFFFFF;

    private void AcquireMacActivityAssertion()
    {
        try
        {
            var nsProcessInfoClass = objc_getClass("NSProcessInfo");
            var processInfo = objc_msgSend(nsProcessInfoClass, sel_registerName("processInfo"));
            if (processInfo == IntPtr.Zero) return;

            var reason = CFStringCreateWithCString(IntPtr.Zero,
                "Battery monitoring requires timely timer firing", kCFStringEncodingUTF8);
            if (reason == IntPtr.Zero) return;

            var activity = objc_msgSend_ulong_IntPtr(processInfo,
                sel_registerName("beginActivityWithOptions:reason:"),
                NSActivityUserInitiatedAllowingIdleSystemSleep, reason);

            if (activity != IntPtr.Zero)
            {
                // CFRetain prevents the ObjC runtime from releasing the activity object.
                // Without this, ARC/autorelease pool could release it and App Nap resumes.
                CFRetain(activity);
                _macActivityAssertion = activity;
                Logger.Debug("macOS App Nap prevention assertion acquired");
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to acquire macOS activity assertion");
        }
    }

    private void ReleaseMacActivityAssertion()
    {
        lock (_lock)
        {
            if (_macActivityAssertion == IntPtr.Zero) return;
            var assertion = _macActivityAssertion;
            _macActivityAssertion = IntPtr.Zero;

            try
            {
                var nsProcessInfoClass = objc_getClass("NSProcessInfo");
                var processInfo = objc_msgSend(nsProcessInfoClass, sel_registerName("processInfo"));
                if (processInfo != IntPtr.Zero)
                    objc_msgSend_IntPtr(processInfo, sel_registerName("endActivity:"), assertion);

                CFRelease(assertion);
                Logger.Debug("macOS App Nap prevention assertion released");
            }
            catch (Exception ex)
            {
                Logger.Debug(ex, "Failed to release macOS activity assertion");
            }
        }
    }

    // ── Linux: nice (CPU scheduling priority) ──
    // Unprivileged users can increase nice (lower priority) but cannot decrease it back.
    // We only increase nice when entering efficiency mode and accept it's one-way.
    // On dispose, we don't attempt to reset (would fail without CAP_SYS_NICE).

    [DllImport("libc", EntryPoint = "setpriority")]
    private static extern int setpriority(int which, int who, int prio);

    private const int PRIO_PROCESS = 0;
    private const int NiceEfficient = 10; // Moderate deprioritization (unprivileged max is 19)

    private static void SetLinuxNice(bool enable)
    {
        if (!enable) return; // Cannot lower nice value without privileges — skip

        try
        {
            var result = setpriority(PRIO_PROCESS, 0, NiceEfficient);
            if (result != 0)
                Logger.Debug("setpriority returned {Result} — nice value may not have changed", result);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to set Linux nice value");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        if (_isEfficient && OperatingSystem.IsWindows())
            SetWindowsEcoQoS(false);

        if (OperatingSystem.IsMacOS())
            ReleaseMacActivityAssertion();
    }
}