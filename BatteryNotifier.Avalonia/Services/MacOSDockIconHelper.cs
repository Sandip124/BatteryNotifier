using System;
using System.Runtime.InteropServices;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// Manages the macOS Dock icon and activation policy.
/// Window.Icon only affects the title bar; the Dock icon is a separate API.
/// Activation policy controls whether the app appears in the Dock at all.
/// </summary>
internal static class MacOSDockIconHelper
{
    private static byte[]? _cachedIconPng;

    /// <summary>
    /// Sets the Dock icon image and caches the PNG data for later re-application.
    /// </summary>
    internal static void SetDockIcon(byte[] pngData)
    {
        if (!OperatingSystem.IsMacOS()) return;

        _cachedIconPng = pngData;
        ApplyIconImage(pngData);
    }

    /// <summary>
    /// Hides the Dock icon by switching to NSApplicationActivationPolicyAccessory.
    /// </summary>
    internal static void HideDockIcon()
    {
        if (!OperatingSystem.IsMacOS()) return;

        try
        {
            var nsApp = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
            // NSApplicationActivationPolicyAccessory = 1
            objc_msgSend_long(nsApp, sel_registerName("setActivationPolicy:"), 1);
        }
        catch
        {
            // Non-critical
        }
    }

    /// <summary>
    /// Shows the Dock icon by switching to NSApplicationActivationPolicyRegular,
    /// then re-applies the cached icon to prevent macOS from resetting it to the
    /// default .NET icon.
    /// </summary>
    internal static void ShowDockIcon()
    {
        if (!OperatingSystem.IsMacOS()) return;

        try
        {
            var nsApp = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
            // NSApplicationActivationPolicyRegular = 0
            objc_msgSend_long(nsApp, sel_registerName("setActivationPolicy:"), 0);

            // Re-apply cached icon — policy change resets it
            if (_cachedIconPng != null)
                ApplyIconImage(_cachedIconPng);
        }
        catch
        {
            // Non-critical
        }
    }

    private static void ApplyIconImage(byte[] pngData)
    {
        try
        {
            var nsDataClass = objc_getClass("NSData");
            var dataWithBytesSel = sel_registerName("dataWithBytes:length:");
            var pinned = GCHandle.Alloc(pngData, GCHandleType.Pinned);

            try
            {
                var nsData = objc_msgSend_bytes_length(
                    nsDataClass, dataWithBytesSel,
                    pinned.AddrOfPinnedObject(), (nuint)pngData.Length);
                if (nsData == IntPtr.Zero) return;

                var nsImageClass = objc_getClass("NSImage");
                var nsImageAlloc = objc_msgSend(nsImageClass, sel_registerName("alloc"));
                var nsImage = objc_msgSend(nsImageAlloc, sel_registerName("initWithData:"), nsData);
                if (nsImage == IntPtr.Zero) return;

                var nsApp = objc_msgSend(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
                objc_msgSend(nsApp, sel_registerName("setApplicationIconImage:"), nsImage);
            }
            finally
            {
                pinned.Free();
            }
        }
        catch
        {
            // Non-critical — generic Dock icon stays
        }
    }

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_getClass(string className);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr sel_registerName(string selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_bytes_length(
        IntPtr receiver, IntPtr selector, IntPtr bytes, nuint length);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_long(IntPtr receiver, IntPtr selector, long arg);
}
