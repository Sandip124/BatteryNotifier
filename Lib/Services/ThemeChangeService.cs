using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BatteryNotifier.Utils;
using Microsoft.Win32;

namespace BatteryNotifier.Lib.Services;

public class ThemeChangeService : IDisposable
{
    private readonly MessageOnlyWindow _messageWindow;
    private bool _currentTheme;
    private bool _disposed;
    
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    
    public ThemeChangeService()
    {
        _currentTheme = UtilityHelper.IsLightTheme();
        _messageWindow = new MessageOnlyWindow(this);
        
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }
    
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle)
        {
            CheckThemeChange();
        }
    }
    
    internal void OnWindowMessage(int msg, IntPtr lParam)
    {
        const int WM_SETTINGCHANGE = 0x001A;
        const int WM_THEMECHANGED = 0x031A;
        
        switch (msg)
        {
            case WM_SETTINGCHANGE:
                string? settingName = Marshal.PtrToStringUni(lParam);
                if (settingName == "ImmersiveColorSet" || string.IsNullOrEmpty(settingName))
                {
                    CheckThemeChange();
                }
                break;
                
            case WM_THEMECHANGED:
                CheckThemeChange();
                break;
        }
    }
    
    private void CheckThemeChange()
    {
        var newTheme = UtilityHelper.IsLightTheme();
        if (newTheme == _currentTheme) return;
        
        _currentTheme = newTheme;
        OnThemeChanged(new ThemeChangedEventArgs(newTheme));
    }
    
    private void OnThemeChanged(ThemeChangedEventArgs e)
    {
        ThemeChanged?.Invoke(this, e);
    }
    
    
    public void Dispose()
    {
        if (_disposed) return;
        
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _messageWindow?.Dispose();
        _disposed = true;
    }
}

// Internal message-only window for receiving Windows messages
internal sealed class MessageOnlyWindow : NativeWindow, IDisposable
{
    private readonly ThemeChangeService _service;
    
    public MessageOnlyWindow(ThemeChangeService service)
    {
        _service = service;
        CreateHandle(new CreateParams
        {
            Parent = new IntPtr(-3)
        });
    }
    
    protected override void WndProc(ref Message m)
    {
        _service.OnWindowMessage(m.Msg, m.LParam);
        base.WndProc(ref m);
    }
    
    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }
    }
}

// Event arguments for theme change
public class ThemeChangedEventArgs : EventArgs
{
    public bool IsLightTheme { get; }
    
    public ThemeChangedEventArgs(bool isLightTheme)
    {
        IsLightTheme = isLightTheme;
    }
}