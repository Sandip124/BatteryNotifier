using System;
using System.Runtime.InteropServices;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>
/// Sets the macOS Dock icon via NSApplication.shared.applicationIconImage.
/// Window.Icon only affects the title bar; the Dock icon is a separate API.
/// When running outside a .app bundle (e.g. dotnet run), macOS ignores
/// Info.plist so this is the only way to get a proper Dock icon.
/// </summary>
internal static class MacOSDockIconHelper
{
    internal static void SetDockIcon(byte[] pngData)
    {
        if (!OperatingSystem.IsMacOS()) return;

        try
        {
            // NSData *data = [NSData dataWithBytes:pngData length:len];
            var nsDataClass = objc_getClass("NSData");
            var dataWithBytesSel = sel_registerName("dataWithBytes:length:");
            var pinned = GCHandle.Alloc(pngData, GCHandleType.Pinned);

            try
            {
                var nsData = objc_msgSend_bytes_length(
                    nsDataClass, dataWithBytesSel,
                    pinned.AddrOfPinnedObject(), (nuint)pngData.Length);
                if (nsData == IntPtr.Zero) return;

                // NSImage *image = [[NSImage alloc] initWithData:data];
                var nsImageClass = objc_getClass("NSImage");
                var nsImageAlloc = objc_msgSend(nsImageClass, sel_registerName("alloc"));
                var nsImage = objc_msgSend(nsImageAlloc, sel_registerName("initWithData:"), nsData);
                if (nsImage == IntPtr.Zero) return;

                // [NSApp setApplicationIconImage:image];
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
}
