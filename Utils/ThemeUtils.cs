using BatteryNotifier.Theming;

namespace BatteryNotifier.Utils;

internal static class ThemeUtils
{
    private static BaseTheme? _currentTheme;
    private static bool? _isDarkTheme;

    public static BaseTheme? GetTheme()
    {
        var settings = Setting.appSetting.Default;

        bool shouldUseDark = settings.SystemThemeApplied
            ? !UtilityHelper.IsLightTheme()
            : settings.darkThemeApplied;

        if (_isDarkTheme != shouldUseDark || _currentTheme == null)
        {
            _currentTheme = shouldUseDark ? new DarkTheme() : new LightTheme();
            _isDarkTheme = shouldUseDark;
        }

        return _currentTheme;
    }

    public static bool IsDarkTheme => _isDarkTheme ?? GetTheme() is DarkTheme;
}