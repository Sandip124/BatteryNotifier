using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using BatteryNotifier.Core.Logger;
using Serilog;

namespace BatteryNotifier.Avalonia.Services;

/// <summary>One entry in the native status-item context menu (optionally a submenu).</summary>
internal readonly record struct MacMenuItem(string Title, int Tag, bool IsSeparator, IReadOnlyList<MacMenuItem>? Submenu)
{
    public static MacMenuItem Separator { get; } = new(string.Empty, -1, true, null);
    public static MacMenuItem Item(string title, int tag) => new(title, tag, false, null);
    public static MacMenuItem Sub(string title, IReadOnlyList<MacMenuItem> children) => new(title, -1, false, children);
}

/// <summary>
/// Native macOS menu-bar status item (NSStatusItem) built via Objective-C interop.
///
/// Avalonia's cross-platform <c>TrayIcon</c> is menu-first on macOS: it shows a menu on click and
/// never fires a click event, so the JetBrains-Toolbox interaction is impossible through it. This
/// wires the status-bar button's target/action directly, so a single <b>left-click toggles the
/// window</b> and a <b>right-click (or control-click) shows a native context menu</b>.
///
/// Best-effort: <see cref="Install"/> returns false on any failure so the caller can fall back to
/// Avalonia's tray icon.
/// </summary>
internal static class MacStatusItem
{
    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("MacStatusItem");

    // AppKit constants (from NSStatusItem / NSEvent headers).
    private const double NSVariableStatusItemLength = -1.0;
    private const long NSEventTypeLeftMouseUp = 2;
    private const long NSEventTypeRightMouseUp = 4;
    private const ulong NSEventMaskLeftMouseUp = 1UL << 2;
    private const ulong NSEventMaskRightMouseUp = 1UL << 4;
    private const ulong NSEventModifierFlagControl = 1UL << 18;

    private static IntPtr _statusItem;
    private static IntPtr _button;
    private static IntPtr _target;

    private static Action? _onLeftClick;
    private static Func<IReadOnlyList<MacMenuItem>>? _menuProvider;
    private static Action<int>? _onMenuItem;

    // Keep the delegates that back the Objective-C IMPs alive for the process lifetime.
    private static readonly ClickHandler ClickImpl = OnStatusItemClicked;
    private static readonly ClickHandler MenuImpl = OnMenuItemClicked;

    public static bool IsInstalled => _statusItem != IntPtr.Zero;

    public static bool Install(
        byte[] iconPng,
        Action onLeftClick,
        Func<IReadOnlyList<MacMenuItem>> menuProvider,
        Action<int> onMenuItem)
    {
        if (!OperatingSystem.IsMacOS()) return false;
        if (IsInstalled) return true;

        try
        {
            _onLeftClick = onLeftClick;
            _menuProvider = menuProvider;
            _onMenuItem = onMenuItem;

            var statusBar = MsgSend(Cls("NSStatusBar"), Sel("systemStatusBar"));
            if (statusBar == IntPtr.Zero) return false;

            var item = MsgSend(statusBar, Sel("statusItemWithLength:"), NSVariableStatusItemLength);
            if (item == IntPtr.Zero) return false;
            CFRetain(item);
            _statusItem = item;

            _button = MsgSend(item, Sel("button"));
            if (_button == IntPtr.Zero) { Uninstall(); return false; }

            SetButtonImage(iconPng, MsgSendDouble(statusBar, Sel("thickness")));

            _target = CreateTarget();
            if (_target == IntPtr.Zero) { Uninstall(); return false; }

            MsgSend(_button, Sel("setTarget:"), _target);
            MsgSend(_button, Sel("setAction:"), Sel("statusItemClicked:"));
            MsgSend(_button, Sel("sendActionOn:"), NSEventMaskLeftMouseUp | NSEventMaskRightMouseUp);

            Logger.Information("Native macOS status item installed");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to install native macOS status item; falling back to Avalonia tray");
            Uninstall();
            return false;
        }
    }

    public static void Uninstall()
    {
        try
        {
            if (_statusItem != IntPtr.Zero)
            {
                var statusBar = MsgSend(Cls("NSStatusBar"), Sel("systemStatusBar"));
                if (statusBar != IntPtr.Zero)
                    MsgSend(statusBar, Sel("removeStatusItem:"), _statusItem);
                CFRelease(_statusItem);
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Error removing native macOS status item");
        }

        _statusItem = IntPtr.Zero;
        _button = IntPtr.Zero;
        _target = IntPtr.Zero;
    }

    private static void SetButtonImage(byte[] png, double thickness)
    {
        if (png.Length == 0) return;

        // Size to the menu-bar thickness (with a 1pt inset) when we got a sane value.
        var size = thickness is >= 16 and <= 48 ? thickness - 1 : 20;

        var handle = GCHandle.Alloc(png, GCHandleType.Pinned);
        try
        {
            var nsData = MsgSend(Cls("NSData"), Sel("dataWithBytes:length:"),
                handle.AddrOfPinnedObject(), (nuint)png.Length);
            if (nsData == IntPtr.Zero) return;

            var image = MsgSend(MsgSend(Cls("NSImage"), Sel("alloc")), Sel("initWithData:"), nsData);
            if (image == IntPtr.Zero) return;

            MsgSend(image, Sel("setSize:"), new CGSize { Width = size, Height = size });
            // Template image → AppKit renders it monochrome and adapts it to the light/dark menu bar.
            MsgSend(image, Sel("setTemplate:"), true);
            MsgSend(_button, Sel("setImage:"), image);
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>
    /// Registers (once) a tiny NSObject subclass whose two selectors forward to our managed
    /// handlers, then returns a retained instance to use as the button/menu target.
    /// </summary>
    private static IntPtr CreateTarget()
    {
        var cls = Cls("BNStatusItemTarget");
        if (cls == IntPtr.Zero)
        {
            cls = objc_allocateClassPair(Cls("NSObject"), "BNStatusItemTarget", UIntPtr.Zero);
            if (cls == IntPtr.Zero) return IntPtr.Zero;

            // "v@:@" = void return; args are self (id), _cmd (SEL), sender (id).
            class_addMethod(cls, Sel("statusItemClicked:"), Marshal.GetFunctionPointerForDelegate(ClickImpl), "v@:@");
            class_addMethod(cls, Sel("menuItemClicked:"), Marshal.GetFunctionPointerForDelegate(MenuImpl), "v@:@");
            objc_registerClassPair(cls);
        }

        var target = MsgSend(MsgSend(cls, Sel("alloc")), Sel("init"));
        if (target != IntPtr.Zero) CFRetain(target);
        return target;
    }

    // ── Native → managed callbacks (invoked by AppKit on the main thread) ──

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ClickHandler(IntPtr self, IntPtr cmd, IntPtr sender);

    private static void OnStatusItemClicked(IntPtr self, IntPtr cmd, IntPtr sender)
    {
        try
        {
            var evt = MsgSend(MsgSend(Cls("NSApplication"), Sel("sharedApplication")), Sel("currentEvent"));
            if (evt == IntPtr.Zero)
            {
                Dispatcher.UIThread.Post(() => _onLeftClick?.Invoke());
                return;
            }

            var type = (long)MsgSendNInt(evt, Sel("type"));
            var controlHeld = ((ulong)MsgSendNInt(evt, Sel("modifierFlags")) & NSEventModifierFlagControl) != 0;

            if (type == NSEventTypeRightMouseUp || (type == NSEventTypeLeftMouseUp && controlHeld))
                ShowMenu(evt);
            else
                Dispatcher.UIThread.Post(() => _onLeftClick?.Invoke());
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Native status item click handler failed");
        }
    }

    private static void OnMenuItemClicked(IntPtr self, IntPtr cmd, IntPtr sender)
    {
        try
        {
            var tag = (int)MsgSendNInt(sender, Sel("tag"));
            Dispatcher.UIThread.Post(() => _onMenuItem?.Invoke(tag));
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Native menu item handler failed");
        }
    }

    private static void ShowMenu(IntPtr evt)
    {
        var items = _menuProvider?.Invoke();
        if (items is not { Count: > 0 }) return;

        var menu = BuildMenu(items);
        if (menu == IntPtr.Zero) return;

        // +[NSMenu popUpContextMenu:withEvent:forView:] — shows the menu without hijacking left-click.
        MsgSend(Cls("NSMenu"), Sel("popUpContextMenu:withEvent:forView:"), menu, evt, _button);
    }

    private static IntPtr BuildMenu(IReadOnlyList<MacMenuItem> items)
    {
        var menu = MsgSend(MsgSend(Cls("NSMenu"), Sel("alloc")), Sel("init"));
        if (menu == IntPtr.Zero) return IntPtr.Zero;

        MsgSend(menu, Sel("setAutoenablesItems:"), false); // keep our explicit targets enabled

        foreach (var entry in items)
        {
            if (entry.IsSeparator)
            {
                MsgSend(menu, Sel("addItem:"), MsgSend(Cls("NSMenuItem"), Sel("separatorItem")));
                continue;
            }

            var title = CFString(entry.Title);
            var empty = CFString(string.Empty);
            try
            {
                // A submenu parent has no action (nil selector) — clicking it just expands it.
                var action = entry.Submenu != null ? IntPtr.Zero : Sel("menuItemClicked:");
                var mi = MsgSend(MsgSend(Cls("NSMenuItem"), Sel("alloc")),
                    Sel("initWithTitle:action:keyEquivalent:"), title, action, empty);

                if (entry.Submenu != null)
                {
                    var sub = BuildMenu(entry.Submenu);
                    if (sub != IntPtr.Zero) MsgSend(mi, Sel("setSubmenu:"), sub);
                }
                else
                {
                    MsgSend(mi, Sel("setTarget:"), _target);
                    MsgSend(mi, Sel("setTag:"), (IntPtr)entry.Tag);
                }

                MsgSend(menu, Sel("addItem:"), mi);
            }
            finally
            {
                CFRelease(title);
                CFRelease(empty);
            }
        }

        return menu;
    }

    // ── Objective-C / CoreFoundation interop ──
    // objc_msgSend has one overload per argument signature (all bind to the same native symbol);
    // a void-returning ObjC method is called through an IntPtr-returning overload and the result ignored.

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr Cls(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr Sel(string name);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, UIntPtr extraBytes);

    [DllImport("/usr/lib/libobjc.dylib")]
    private static extern void objc_registerClassPair(IntPtr cls);

    [DllImport("/usr/lib/libobjc.dylib")]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool class_addMethod(IntPtr cls, IntPtr sel, IntPtr imp, string types);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend(IntPtr receiver, IntPtr sel);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend(IntPtr receiver, IntPtr sel, IntPtr a);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend(IntPtr receiver, IntPtr sel, IntPtr a, IntPtr b, IntPtr c);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend(IntPtr receiver, IntPtr sel, IntPtr bytes, nuint length);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend(IntPtr receiver, IntPtr sel, double arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend(IntPtr receiver, IntPtr sel, ulong arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend(IntPtr receiver, IntPtr sel, [MarshalAs(UnmanagedType.I1)] bool arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr MsgSend(IntPtr receiver, IntPtr sel, CGSize arg);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern nint MsgSendNInt(IntPtr receiver, IntPtr sel);

    // CGFloat return (arm64 uses objc_msgSend for float/double returns).
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern double MsgSendDouble(IntPtr receiver, IntPtr sel);

    private const uint kCFStringEncodingUTF8 = 0x08000100;

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFStringCreateWithCString(IntPtr allocator, string cStr, uint encoding);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFRetain(IntPtr cf);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    private static IntPtr CFString(string s) => CFStringCreateWithCString(IntPtr.Zero, s, kCFStringEncodingUTF8);

    [StructLayout(LayoutKind.Sequential)]
    private struct CGSize
    {
        public double Width;
        public double Height;
    }
}
