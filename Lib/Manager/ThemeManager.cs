using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using BatteryNotifier.Lib.CustomControls.FlatTabControl;
using BatteryNotifier.Properties;
using BatteryNotifier.Theming;
using BatteryNotifier.Utils;
using NuGet;
using appSetting = BatteryNotifier.Setting.appSetting;

namespace BatteryNotifier.Lib.Manager
{
    public class ThemeManager : IDisposable
    {
        private bool _disposed;
        private bool _isApplyingTheme;

        private readonly HashSet<Control>? foregroundControls = new();
        private readonly HashSet<Control>? accentControls = new();
        private readonly HashSet<Control>? accent2Controls = new();
        private readonly HashSet<Control>? accent3Controls = new();
        private readonly HashSet<FlatTabControl>? borderdCustomControls = new();
        private readonly Dashboard dashboard;

        public ThemeManager(Dashboard dashboard)
        {
            this.dashboard = dashboard;
        }

        public ThemeManager RegisterForegroundControls(Control[] controls)
        {
            foreach (var control in controls)
            {
                foregroundControls?.Add(control);
            }
            return this;
        }

        public ThemeManager RegisterAccentControls(Control[] controls)
        {
            foreach (var control in controls)
            {
                accentControls?.Add(control);
            }
            return this;
        }

        public ThemeManager RegisterAccent2Controls(Control[] controls)
        {
            foreach (var control in controls)
            {
                accent2Controls?.Add(control);
            }
            return this;
        }

        public ThemeManager RegisterAccent3Controls(Control[] controls)
        {
            foreach (var control in controls)
            {
                accent3Controls?.Add(control);
            }
            return this;
        }
        
        public ThemeManager RegisterBorderedCustomControls(FlatTabControl[] controls)
        {
            foreach (var control in controls)
            {
                borderdCustomControls?.Add(control);
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
            ApplyBorderColor(borderdCustomControls, theme.BorderColor);
        }

        private void ApplyThemeImages(PictureBox themePictureBox, PictureBox closeIcon)
        {
            var desiredImage = ThemeUtils.IsDarkTheme ? ImageCache.DarkMode : ImageCache.LightMode;
            if (themePictureBox.Image != desiredImage)
            {
                themePictureBox.Image = desiredImage;
            }

            if (closeIcon.Image != ImageCache.CloseIconDark)
            {
                closeIcon.Image = ImageCache.CloseIconDark;
            }
        }

        private void ApplyBackgroundColor(HashSet<Control>? controls, Color color)
        {
            if (controls == null || controls.Count == 0) return;
            foreach (var control in controls)
            {
                if (control != null && control.IsDisposed == false && control.BackColor != color)
                {
                    control.BackColor = color;
                }
            }
        }

        private void ApplyForegroundColor(HashSet<Control>? controls, Color color)
        {
            if (controls == null || controls.Count == 0) return;
            foreach (var control in controls)
            {
                if (control != null && control.IsDisposed == false && control.ForeColor != color)
                {
                    control.ForeColor = color;
                }
            }
        }
        
        private void ApplyBorderColor(HashSet<FlatTabControl>? controls, Color color)
        {
            if (controls == null || controls.Count == 0) return;
            foreach (var control in controls)
            {
                if (control != null && control.IsDisposed == false && control.ForeColor != color)
                {
                    control.BorderColor = color;
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
        
        static class ImageCache
        {
            public static readonly Image DarkMode = Resources.DarkMode;
            public static readonly Image LightMode = Resources.LightMode;
            public static readonly Image CloseIconDark = Resources.closeIconDark;
        }
    }
    
    
}