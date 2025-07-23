using BatteryNotifier.Theming;

namespace BatteryNotifier.Utils
{
    internal static class ThemeUtils
    {
        public static BaseTheme GetTheme()
        {
            var settings = Setting.appSetting.Default;

            if (settings.SystemThemeApplied)
            {
                return ApplyDarkTheme(!UtilityHelper.IsLightTheme());
            }

            return ApplyDarkTheme(settings.darkThemeApplied);
        }
        
        public static bool IsDarkTheme => GetTheme() is DarkTheme;

        private static BaseTheme ApplyDarkTheme(bool useDark) =>
            useDark ? new DarkTheme() : new LightTheme();
    }

}