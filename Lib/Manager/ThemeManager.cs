using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using BatteryNotifier.Lib.CustomControls.FlatTabControl;
using BatteryNotifier.Properties;
using BatteryNotifier.Theming;
using BatteryNotifier.Utils;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Lib.Manager
{
    public class ThemeManager(Dashboard dashboard) : IDisposable
    {
        private bool _disposed;
        private bool _isApplyingTheme;

        private readonly HashSet<Control>? foregroundControls = [];
        private readonly HashSet<Control>? accentControls = [];
        private readonly HashSet<Control>? accent2Controls = [];
        private readonly HashSet<Control>? accent3Controls = [];
        private readonly HashSet<FlatTabControl>? flatTabCustomControls = [];

        public ThemeManager RegisterForegroundControls(Control[] controls) =>
            RegisterControls(controls, foregroundControls);
        public ThemeManager RegisterAccentControls(Control[] controls) => RegisterControls(controls, accentControls);
        public ThemeManager RegisterAccent2Controls(Control[] controls) => RegisterControls(controls, accent2Controls);
        public ThemeManager RegisterAccent3Controls(Control[] controls) => RegisterControls(controls, accent3Controls);
        public ThemeManager RegisterBorderedCustomControls(FlatTabControl[] controls) => RegisterControls(controls, flatTabCustomControls);
        
        private ThemeManager RegisterControls<T>(T[] controls,HashSet<T>? controlHolder)
        {
            foreach (var control in controls)
            {
                controlHolder?.Add(control);
            }
            return this;
        }

        public ThemeManager SetSystemTheme()
        {
            appSetting.Default.darkThemeApplied = false;
            appSetting.Default.SystemThemeApplied = true;
            appSetting.Default.Save();
            return this;
        }

        public ThemeManager SetDarkTheme()
        {
            appSetting.Default.darkThemeApplied = true;
            appSetting.Default.SystemThemeApplied = false;
            appSetting.Default.Save();
            return this;
        }

        public ThemeManager SetLightTheme()
        {
            appSetting.Default.darkThemeApplied = false;
            appSetting.Default.SystemThemeApplied = false;
            appSetting.Default.Save();
            return this;
        }

        public void ApplyTheme(PictureBox pictureBox, PictureBox closeIcon)
        {
            if (_disposed || _isApplyingTheme)
            {
                return;
            }
            
            _isApplyingTheme = true;
            dashboard.SuspendLayout();
            
            try
            {
                ApplyThemeColors(ThemeUtils.GetTheme());
                ApplyThemeImages(pictureBox, closeIcon);
            }
            finally
            {
                dashboard.ResumeLayout(false);
                _isApplyingTheme = false;
            }
        }

        private void ApplyThemeColors(BaseTheme? theme)
        {
            if (theme == null) return;
            
            var accent = theme.AccentColor;
            var accent2 = theme.Accent2Color;
            var accent3 = theme.Accent3Color;
            var foreground = theme.ForegroundColor;

            ApplyBackgroundColor(accentControls, accent);
            ApplyBackgroundColor(accent2Controls, accent2);
            ApplyBackgroundColor(accent3Controls, accent3);
            ApplyForegroundColor(foregroundControls, foreground);
            ApplyBorderColor(flatTabCustomControls, theme.BorderColor);
        }

        private void ApplyThemeImages(PictureBox themePictureBox, PictureBox closeIcon)
        {
            //TODO: more optimize this 
            var desiredImage = ThemeUtils.IsDarkTheme ? ImageCache.DarkMode : ImageCache.LightMode;
            
            UtilityHelper.SafeInvoke(themePictureBox, () =>
            {
                if (themePictureBox.Image != desiredImage)
                {
                    themePictureBox.Image = desiredImage;
                }
            });
            
            UtilityHelper.SafeInvoke(closeIcon, () =>
            {
                if (closeIcon.Image != ImageCache.CloseIconDark)
                {
                    closeIcon.Image = ImageCache.CloseIconDark;
                }
            });
        }

        private static void ApplyBackgroundColor(HashSet<Control>? controls, Color color)
        {
            if (controls == null || controls.Count == 0) return;
            foreach (var control in controls)
            {
                if (control is { IsDisposed: false } && control.BackColor != color)
                {
                    UtilityHelper.SafeInvoke(control, () =>
                    {
                        control.BackColor = color;
                    });
                }
            }
        }

        private static void ApplyForegroundColor(HashSet<Control>? controls, Color color)
        {
            if (controls == null || controls.Count == 0) return;
            foreach (var control in controls)
            {
                if (control is { IsDisposed: false } && control.ForeColor != color)
                {
                    UtilityHelper.SafeInvoke(control, () =>
                    {
                        control.ForeColor = color;
                    });
                }
            }
        }
        
        private static void ApplyBorderColor(HashSet<FlatTabControl>? controls, Color color)
        {
            if (controls == null || controls.Count == 0) return;
            foreach (var control in controls)
            {
                if (control is { IsDisposed: false } && control.ForeColor != color)
                {
                    UtilityHelper.SafeInvoke(control, () =>
                    {
                        control.BorderColor = color;
                    });
                }
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _disposed = true;
            }
        }

        private static class ImageCache
        {
            public static readonly Image DarkMode = Resources.DarkMode;
            public static readonly Image LightMode = Resources.LightMode;
            public static readonly Image CloseIconDark = Resources.closeIconDark;
        }
    }
    
    
}