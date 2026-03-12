using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

[Collection("AppSettings")]
public class AppSettingsTests
{
    [Fact]
    public void Instance_ReturnsNonNull()
    {
        var settings = AppSettings.Instance;
        Assert.NotNull(settings);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var settings = AppSettings.Instance;
        // Reset to defaults first
        settings.Reset();

        Assert.True(settings.FullBatteryNotification);
        Assert.True(settings.LowBatteryNotification);
        Assert.Equal(96, settings.FullBatteryNotificationValue);
        Assert.Equal(25, settings.LowBatteryNotificationValue);
        Assert.Null(settings.FullBatteryNotificationMusic);
        Assert.Null(settings.LowBatteryNotificationMusic);
        Assert.True(settings.StartMinimized);
        Assert.Equal(ThemeMode.System, settings.ThemeMode);
        Assert.True(settings.LaunchAtStartup);
    }

    [Fact]
    public void Reset_RestoresDefaults()
    {
        var settings = AppSettings.Instance;

        // Modify settings
        settings.FullBatteryNotification = false;
        settings.LowBatteryNotificationValue = 10;
        settings.ThemeMode = ThemeMode.Dark;
        settings.Save();

        // Reset
        settings.Reset();

        Assert.True(settings.FullBatteryNotification);
        Assert.Equal(25, settings.LowBatteryNotificationValue);
        Assert.Equal(ThemeMode.System, settings.ThemeMode);
    }

    [Fact]
    public void Save_ThenLoad_PreservesValues()
    {
        var settings = AppSettings.Instance;
        settings.Reset();

        settings.FullBatteryNotificationValue = 85;
        settings.LowBatteryNotificationValue = 15;
        settings.ThemeMode = ThemeMode.Light;
        settings.Save();

        settings.Load();

        Assert.Equal(85, settings.FullBatteryNotificationValue);
        Assert.Equal(15, settings.LowBatteryNotificationValue);
        Assert.Equal(ThemeMode.Light, settings.ThemeMode);

        // Cleanup - restore defaults
        settings.Reset();
    }
}
