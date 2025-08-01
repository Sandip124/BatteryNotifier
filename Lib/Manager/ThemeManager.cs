using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BatteryNotifier.Forms;
using BatteryNotifier.Lib.CustomControls.FlatTabControl;
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

        public ThemeManager RegisterBorderedCustomControls(FlatTabControl[] controls) =>
            RegisterControls(controls, flatTabCustomControls);

        private ThemeManager RegisterControls<T>(T[] controls, HashSet<T>? controlHolder)
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

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
        private const int WM_SETREDRAW = 11;

        public void ApplyTheme()
        {
            if (_disposed || _isApplyingTheme)
            {
                return;
            }

            _isApplyingTheme = true;
            SendMessage(dashboard.Handle, WM_SETREDRAW, false, 0);
            dashboard.SuspendLayout();

            try
            {
                ApplyThemeColors(ThemeUtils.GetTheme());
            }
            finally
            {
                dashboard.ResumeLayout(true);
                SendMessage(dashboard.Handle, WM_SETREDRAW, true, 0);
                _isApplyingTheme = false;
            }
        }

        private void ApplyThemeColors(BaseTheme? theme)
        {
            if (theme == null) return;

            var controlThemeMap = new Dictionary<Control, ThemeProperties>();

            AddControlsToMap(controlThemeMap, accentControls, new ThemeProperties { BackColor = theme.AccentColor });
            AddControlsToMap(controlThemeMap, accent2Controls, new ThemeProperties { BackColor = theme.Accent2Color });
            AddControlsToMap(controlThemeMap, accent3Controls, new ThemeProperties { BackColor = theme.Accent3Color });
            AddControlsToMap(controlThemeMap, foregroundControls,
                new ThemeProperties { ForeColor = theme.ForegroundColor });
            AddControlsToMap(controlThemeMap, flatTabCustomControls,
                new ThemeProperties { BorderColor = theme.BorderColor });

            // Disable redraw for batch update
            SendMessage(dashboard.Handle, WM_SETREDRAW, false, 0);
            dashboard.SuspendLayout();

            try
            {
                foreach (var kvp in controlThemeMap)
                {
                    var control = kvp.Key;
                    if (control is { IsDisposed: false })
                    {
                        UtilityHelper.SafeInvoke(control, () =>
                        {
                            var themeProps = kvp.Value;

                            if (themeProps.BackColor.HasValue && control.BackColor != themeProps.BackColor.Value)
                                control.BackColor = themeProps.BackColor.Value;

                            if (themeProps.ForeColor.HasValue && control.ForeColor != themeProps.ForeColor.Value)
                                control.ForeColor = themeProps.ForeColor.Value;

                            if (themeProps.BorderColor.HasValue && control is FlatTabControl flatTab &&
                                flatTab.BorderColor != themeProps.BorderColor.Value)
                                flatTab.BorderColor = themeProps.BorderColor.Value;
                        });
                    }
                }
            }
            finally
            {
                dashboard.ResumeLayout(true);
                // Re-enable redraw and invalidate the dashboard to refresh UI
                SendMessage(dashboard.Handle, WM_SETREDRAW, true, 0);
                dashboard.Invalidate(true);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed || !disposing) return;

            foregroundControls?.Clear();
            accentControls?.Clear();
            accent2Controls?.Clear();
            accent3Controls?.Clear();
            flatTabCustomControls?.Clear();

            _disposed = true;
        }

        private static void AddControlsToMap(Dictionary<Control, ThemeProperties> map, IEnumerable<Control>? controls,
            ThemeProperties props)
        {
            if (controls == null) return;
            
            foreach (var control in controls)
            {
                if (map.ContainsKey(control))
                {
                    map[control].Merge(props);
                }
                else
                {
                    map[control] = props;
                }
            }
        }

        public class ThemeProperties
        {
            public Color? BackColor { get; set; }
            public Color? ForeColor { get; set; }
            public Color? BorderColor { get; set; }

            public void Merge(ThemeProperties other)
            {
                BackColor = BackColor ?? other.BackColor;
                ForeColor = ForeColor ?? other.ForeColor;
                BorderColor = BorderColor ?? other.BorderColor;
            }
        }
    }
}