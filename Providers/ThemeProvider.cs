using BatteryNotifier.Helpers;
using BatteryNotifier.Theming;

namespace BatteryNotifier.Providers
{
    internal abstract class ThemeProvider
    {
        public static BaseTheme GetTheme()
        {
            var systemThemeApplied = Setting.appSetting.Default.SystemThemeApplied;
            var darkThemeApplied = Setting.appSetting.Default.darkThemeApplied;

            return systemThemeApplied
                ? ApplyDarkTheme(!UtilityHelper.IsLightTheme())
                : ApplyDarkTheme(darkThemeApplied);
        }

        private static BaseTheme ApplyDarkTheme(bool applyDarkTheme) => applyDarkTheme ? new DarkTheme() : new LightTheme();

        public static bool IsDarkTheme => typeof(DarkTheme) == GetTheme().GetType();
    }
}