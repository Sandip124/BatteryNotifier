using System.Runtime.InteropServices;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Manages OS-level power efficiency hints for the app.
/// - Windows: EcoQoS via SetProcessInformation (efficiency cores + lower frequency)
/// - macOS: App Nap prevention via NSProcessInfo activity assertion (keeps timers on time)
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

    // macOS: retained activity assertion handle (prevents App Nap)
    private IntPtr _macActivityAssertion = IntPtr.Zero;

    private EfficiencyModeService()
    {
        // macOS: immediately claim an activity assertion to prevent App Nap
        // from throttling our 1s battery polling timer. This is held for the
        // lifetime of the app — App Nap would otherwise delay timers by 10s+.
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
        }

        if (OperatingSystem.IsWindows())
            SetWindowsEcoQoS(true);
        else if (OperatingSystem.IsLinux())
            SetLinuxNice(true);

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
        }

        if (OperatingSystem.IsWindows())
            SetWindowsEcoQoS(false);
        else if (OperatingSystem.IsLinux())
            SetLinuxNice(false);

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
        }

        DisableEfficiency();
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

    // CFString helpers for creating the reason string
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
            // [NSProcessInfo processInfo]
            var nsProcessInfoClass = objc_getClass("NSProcessInfo");
            var processInfo = objc_msgSend(nsProcessInfoClass, sel_registerName("processInfo"));
            if (processInfo == IntPtr.Zero) return;

            // Create NSString reason
            var reason = CFStringCreateWithCString(IntPtr.Zero,
                "Battery monitoring requires timely timer firing", kCFStringEncodingUTF8);
            if (reason == IntPtr.Zero) return;

            // [processInfo beginActivityWithOptions:reason:]
            var activity = objc_msgSend_ulong_IntPtr(processInfo,
                sel_registerName("beginActivityWithOptions:reason:"),
                NSActivityUserInitiatedAllowingIdleSystemSleep, reason);

            if (activity != IntPtr.Zero)
            {
                // Prevent GC from collecting the activity token — must stay alive
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
        if (_macActivityAssertion == IntPtr.Zero) return;

        try
        {
            var nsProcessInfoClass = objc_getClass("NSProcessInfo");
            var processInfo = objc_msgSend(nsProcessInfoClass, sel_registerName("processInfo"));
            if (processInfo == IntPtr.Zero) return;

            // [processInfo endActivity:_macActivityAssertion]
            objc_msgSend_IntPtr(processInfo, sel_registerName("endActivity:"), _macActivityAssertion);
            _macActivityAssertion = IntPtr.Zero;
            Logger.Debug("macOS App Nap prevention assertion released");
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to release macOS activity assertion");
        }
    }

    // ── Linux: nice (CPU scheduling priority) ──

    // setpriority(PRIO_PROCESS, 0, nice_value) — 0 = current process
    [DllImport("libc", EntryPoint = "setpriority")]
    private static extern int setpriority(int which, int who, int prio);

    private const int PRIO_PROCESS = 0;
    private const int NiceEfficient = 15;  // Low priority but not starved
    private const int NiceNormal = 0;

    private static void SetLinuxNice(bool efficient)
    {
        try
        {
            setpriority(PRIO_PROCESS, 0, efficient ? NiceEfficient : NiceNormal);
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to set Linux nice value");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        if (_isEfficient)
        {
            if (OperatingSystem.IsWindows())
                SetWindowsEcoQoS(false);
            else if (OperatingSystem.IsLinux())
                SetLinuxNice(false);
        }

        if (OperatingSystem.IsMacOS())
            ReleaseMacActivityAssertion();
    }
}