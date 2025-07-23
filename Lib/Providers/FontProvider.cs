using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace BatteryNotifier.Lib.Providers;

internal  class FontProvider : IDisposable
{
    [DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

    [DllImport("gdi32.dll")]
    private static extern bool RemoveFontMemResourceEx(IntPtr handle);

    private readonly PrivateFontCollection _fontsCollection = new();
    private readonly List<IntPtr> _fontHandles = new();
    private readonly Dictionary<float, Font> _regularFontCache = new();
    private readonly Dictionary<float, Font> _boldFontCache = new();
    public static string FontFamilyName => _default._fontsCollection.Families[0].Name;

    private static readonly FontProvider _default = new();

    private FontProvider()
    {
        LoadFont(Properties.Resources.Inter_Regular);
        LoadFont(Properties.Resources.Inter_Bold);
    }

    private void LoadFont(byte[] fontResource)
    {
        var fontPtr = Marshal.AllocCoTaskMem(fontResource.Length);
        Marshal.Copy(fontResource, 0, fontPtr, fontResource.Length);
        uint dummy = 0;
        _fontsCollection.AddMemoryFont(fontPtr, fontResource.Length);
        IntPtr handle = AddFontMemResourceEx(fontPtr, (uint)fontResource.Length, IntPtr.Zero, ref dummy);
        _fontHandles.Add(handle);
        Marshal.FreeCoTaskMem(fontPtr);
    }

    public static Font GetRegularFont(float size = 8)
    {
        return _default.GetOrCreateFont(size, FontStyle.Regular, _default._regularFontCache);
    }

    public static Font GetBoldFont(float size = 8)
    {
        return _default.GetOrCreateFont(size, FontStyle.Bold, _default._boldFontCache);
    }

    private Font GetOrCreateFont(float size, FontStyle style, Dictionary<float, Font> cache)
    {
        if (!cache.TryGetValue(size, out var font))
        {
            font = new Font(_fontsCollection.Families[0], size, style, GraphicsUnit.Point);
            cache[size] = font;
        }
        return font;
    }

    public void Dispose()
    {
        foreach (var font in _regularFontCache.Values)
        {
            font.Dispose();
        }

        foreach (var font in _boldFontCache.Values)
        {
            font.Dispose();
        }

        _regularFontCache.Clear();
        _boldFontCache.Clear();

        foreach (var handle in _fontHandles)
        {
            RemoveFontMemResourceEx(handle);
        }

        _fontHandles.Clear();

        _fontsCollection.Dispose();
    }
}