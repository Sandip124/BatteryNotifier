using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace BatteryNotifier.Lib.Providers;

internal class FontProvider : IDisposable
{
    private bool _disposed;
    
    [DllImport("gdi32.dll")]
    private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, uint cbFont, IntPtr pdv, [In] ref uint pcFonts);

    [DllImport("gdi32.dll")]
    private static extern bool RemoveFontMemResourceEx(IntPtr handle);

    private readonly PrivateFontCollection _fontsCollection = new();
    private readonly List<IntPtr> _fontHandles = new();
    private readonly ConcurrentDictionary<(float Size, FontStyle Style), Font> _fontCache = new();

    private static readonly Lazy<FontProvider> _default = new(() => new FontProvider());
    
    public static string FontFamilyName => _default.Value._fontsCollection.Families[0].Name;

    public static readonly Font DefaultRegularFont = _default.Value.GetOrCreateFont(10.2F, FontStyle.Regular);

    private FontProvider()
    {
        LoadFont(Properties.Resources.Inter_Regular);
        LoadFont(Properties.Resources.Inter_Bold);
    }

    private void LoadFont(byte[] fontResource)
    {
        IntPtr fontPtr = Marshal.AllocCoTaskMem(fontResource.Length);
        Marshal.Copy(fontResource, 0, fontPtr, fontResource.Length);
        uint dummy = 0;
        _fontsCollection.AddMemoryFont(fontPtr, fontResource.Length);
        IntPtr handle = AddFontMemResourceEx(fontPtr, (uint)fontResource.Length, IntPtr.Zero, ref dummy);
        _fontHandles.Add(handle);
        Marshal.FreeCoTaskMem(fontPtr);
    }

    public static Font GetFont(float size = 8F, FontStyle style = FontStyle.Regular)
    {
        var instance = _default.Value;
        if (instance._disposed)
            throw new ObjectDisposedException(nameof(FontProvider));
        
        return instance.GetOrCreateFont(size, style);
    }

    private Font GetOrCreateFont(float size, FontStyle style)
    {
        var key = (size, style);
        if (!_fontCache.TryGetValue(key, out var font))
        {
            font = new Font(_fontsCollection.Families[0], size, style, GraphicsUnit.Point);
            _fontCache[key] = font;
        }
        return font;
    }
    
    public static void Cleanup()
    {
        if (_default.IsValueCreated)
        {
            _default.Value.Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        
        foreach (var font in _fontCache.Values)
        {
            font.Dispose();
        }
        _fontCache.Clear();

        foreach (var handle in _fontHandles)
        {
            if (handle != IntPtr.Zero)
            {
                RemoveFontMemResourceEx(handle);
            }
        }
        _fontHandles.Clear();

        _fontsCollection.Dispose();
    
        _disposed = true;
    }
}
