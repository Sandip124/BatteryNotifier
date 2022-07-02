using BatteryNotifier.Helpers;
using BatteryNotifier.Theming;

namespace BatteryNotifier.Providers
{
    internal class ThemeProvider
    {
        public static BaseTheme GetTheme()
        {
            var systemThemeApplied = Setting.appSetting.Default.SystemThemeApplied;

            BaseTheme theme;
            if (systemThemeApplied)
            {
                if (UtilityHelper.IsLightTheme())
                {
                    theme =  new LightTheme();
                }
                else
                {
                    theme = new DarkTheme();
                }
            }
            else
            {
                if (Setting.appSetting.Default.darkThemeApplied)
                {
                    theme = new DarkTheme();
                }
                else
                {
                    theme = new LightTheme();
                }
            }
            return theme;
        }

        public static bool IsDarkTheme => typeof(DarkTheme) == GetTheme().GetType();
    }
}
