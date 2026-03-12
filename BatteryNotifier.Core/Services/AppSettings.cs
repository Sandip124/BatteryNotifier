using System;
using System.IO;
using System.Text.Json;

namespace BatteryNotifier.Core.Services;

public sealed class AppSettings
{
    private static readonly Lazy<AppSettings> _instance = new(() => new AppSettings());
    public static AppSettings Instance => _instance.Value;

    private const string SettingsFileName = "appsettings.json";
    private string SettingsFilePath => Path.Combine(GetSettingsDirectory(), SettingsFileName);

    // Notification Settings
    public bool FullBatteryNotification { get; set; } = true;
    public bool LowBatteryNotification { get; set; } = true;
    public int FullBatteryNotificationValue { get; set; } = 96;
    public int LowBatteryNotificationValue { get; set; } = 25;
    public string? FullBatteryNotificationMusic { get; set; }
    public string? LowBatteryNotificationMusic { get; set; }

    // Window Settings
    public bool StartMinimized { get; set; } = true;

    // Theme Settings
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    // Startup Settings
    public bool LaunchAtStartup { get; set; } = true;

    // App Identity
    public string AppId { get; set; } = Guid.NewGuid().ToString();

    private AppSettings()
    {
        Load();
    }

    private string GetSettingsDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDir = Path.Combine(appData, "BatteryNotifier");

        if (!Directory.Exists(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }

        return settingsDir;
    }

    public void Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                Save(); // Create default settings
                return;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);

            if (settings != null)
            {
                // Copy properties
                FullBatteryNotification = settings.FullBatteryNotification;
                LowBatteryNotification = settings.LowBatteryNotification;
                FullBatteryNotificationValue = settings.FullBatteryNotificationValue;
                LowBatteryNotificationValue = settings.LowBatteryNotificationValue;
                FullBatteryNotificationMusic = settings.FullBatteryNotificationMusic;
                LowBatteryNotificationMusic = settings.LowBatteryNotificationMusic;
                StartMinimized = settings.StartMinimized;
                ThemeMode = settings.ThemeMode;
                LaunchAtStartup = settings.LaunchAtStartup;
                AppId = settings.AppId;
            }
        }
        catch (Exception)
        {
            // If settings are corrupted, use defaults
            Save();
        }
    }

    public void Save()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(this, options);
            var tmpPath = SettingsFilePath + ".tmp";
            File.WriteAllText(tmpPath, json);
            File.Move(tmpPath, SettingsFilePath, overwrite: true);
        }
        catch (Exception)
        {
            // Fail silently - settings won't persist
        }
    }

    public void Reset()
    {
        FullBatteryNotification = true;
        LowBatteryNotification = true;
        FullBatteryNotificationValue = 96;
        LowBatteryNotificationValue = 25;
        FullBatteryNotificationMusic = null;
        LowBatteryNotificationMusic = null;
        StartMinimized = true;
        ThemeMode = ThemeMode.System;
        LaunchAtStartup = true;

        Save();
    }
}

public enum ThemeMode
{
    System,
    Light,
    Dark
}
