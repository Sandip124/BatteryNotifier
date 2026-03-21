using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using BatteryNotifier.Core.Logger;
using BatteryNotifier.Core.Models;
using Serilog;

namespace BatteryNotifier.Core.Services;

public sealed class AppSettings
{
    private static readonly Lazy<AppSettings> _instance = new(CreateAndLoad);
    public static AppSettings Instance => _instance.Value;

    private static readonly ILogger Logger = BatteryNotifierAppLogger.ForContext("AppSettings");

    private const string SettingsFileName = "appsettings.json";
    private readonly Lock _saveLock = new();
    private static string SettingsFilePath => Path.Combine(GetSettingsDirectory(), SettingsFileName);

    // Notification Settings
    public bool FullBatteryNotification { get; set; } = true;
    public bool LowBatteryNotification { get; set; } = true;
    public int FullBatteryNotificationValue { get; set; } = 96;
    public int LowBatteryNotificationValue { get; set; } = 25;
    public string? FullBatteryNotificationMusic { get; set; } = "builtin:Harp";
    public string? LowBatteryNotificationMusic { get; set; } = "builtin:Klaxon";

    // Window Settings
    public bool StartMinimized { get; set; } = true;
    public int? WindowPositionX { get; set; }
    public int? WindowPositionY { get; set; }

    // Theme Settings
    public ThemeMode ThemeMode { get; set; } = ThemeMode.System;

    // Startup Settings
    public bool LaunchAtStartup { get; set; } = true;

    // Update Settings
    public bool AutoCheckForUpdates { get; set; } = true;

    // Multi-level alerts
    public List<BatteryAlert> Alerts { get; set; } = new();
    public int SettingsVersion { get; set; } = 1;

    // Screen flash for Avalonia-native notifications
    public bool ScreenFlashEnabled { get; set; } = true;

    // Notification card position on screen
    public NotificationPosition NotificationPosition { get; set; } = NotificationPosition.TopCenter;

    // App Identity
    public string AppId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Used by the JSON deserializer — must NOT call Load() to avoid infinite recursion.</summary>
    [JsonConstructor]
    internal AppSettings() { }

    private static AppSettings CreateAndLoad()
    {
        var settings = new AppSettings();
        settings.Load();
        return settings;
    }

    private static string GetSettingsDirectory()
    {
        var settingsDir = Constants.AppDataDirectory;

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
                Logger.Information("No settings file found — creating defaults at {Path}", SettingsFilePath);
                Alerts = CreateDefaultAlerts();
                SettingsVersion = 2;
                Save();
                return;
            }

            var rawBytes = File.ReadAllBytes(SettingsFilePath);
            string json;

            if (SettingsEncryption.IsPlaintext(rawBytes))
            {
                // Legacy plaintext — migrate to encrypted on next save
                json = System.Text.Encoding.UTF8.GetString(rawBytes);
            }
            else
            {
                // Encrypted — decrypt with AES-GCM (throws if tampered)
                json = SettingsEncryption.Decrypt(rawBytes, GetSettingsDirectory());
            }

            var settings = JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings);

            if (settings != null)
            {
                FullBatteryNotification = settings.FullBatteryNotification;
                LowBatteryNotification = settings.LowBatteryNotification;
                FullBatteryNotificationValue = settings.FullBatteryNotificationValue;
                LowBatteryNotificationValue = settings.LowBatteryNotificationValue;
                FullBatteryNotificationMusic = SanitizeSoundPath(settings.FullBatteryNotificationMusic);
                LowBatteryNotificationMusic = SanitizeSoundPath(settings.LowBatteryNotificationMusic);
                StartMinimized = settings.StartMinimized;
                WindowPositionX = settings.WindowPositionX;
                WindowPositionY = settings.WindowPositionY;
                ThemeMode = settings.ThemeMode;
                LaunchAtStartup = settings.LaunchAtStartup;
                AutoCheckForUpdates = settings.AutoCheckForUpdates;
                ScreenFlashEnabled = settings.ScreenFlashEnabled;
                SettingsVersion = settings.SettingsVersion;
                Alerts = settings.Alerts ?? new List<BatteryAlert>();
                AppId = settings.AppId;

                // Migrate v1 → v2: convert flat thresholds to alerts
                if (SettingsVersion < 2)
                {
                    MigrateToAlerts();
                }

                // Sanitize alert sounds
                foreach (var alert in Alerts)
                {
                    alert.Sound = SanitizeSoundPath(alert.Sound);
                }

                Logger.Information("Settings loaded: v{Version}, {AlertCount} alerts", SettingsVersion, Alerts.Count);
            }

            // Re-save to encrypt if it was plaintext (migration)
            if (SettingsEncryption.IsPlaintext(rawBytes))
            {
                Save();
            }
        }
        catch (CryptographicException ex)
        {
            Logger.Warning(ex, "Settings decryption failed (tampered or corrupt) — resetting to defaults. Path: {Path}", SettingsFilePath);
            Reset();
        }
        catch (JsonException ex)
        {
            Logger.Warning(ex, "Settings JSON is corrupt after decryption — resetting to defaults. Path: {Path}", SettingsFilePath);
            Reset();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to load settings — resetting to defaults. Path: {Path}", SettingsFilePath);
            Reset();
        }
    }

    public void Save()
    {
        lock (_saveLock)
        {
            try
            {
                var json = JsonSerializer.Serialize(this, AppSettingsJsonContext.Default.AppSettings);
                var encrypted = SettingsEncryption.Encrypt(json, GetSettingsDirectory());

                var tmpPath = SettingsFilePath + ".tmp";
                File.WriteAllBytes(tmpPath, encrypted);
                File.Move(tmpPath, SettingsFilePath, overwrite: true);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to save settings — changes won't persist");
            }
        }
    }

    /// <summary>
    /// Validates sound paths loaded from settings JSON.
    /// Allows built-in sounds ("builtin:...") and absolute canonical file paths.
    /// Rejects anything else to prevent path traversal or injection from tampered settings.
    /// </summary>
    private static string? SanitizeSoundPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // Built-in sounds are safe — resolved via dictionary lookup
        if (path.StartsWith("builtin:", StringComparison.Ordinal))
            return path;

        // Custom library sounds — resolved via CustomSoundsLibrary
        if (path.StartsWith("custom:", StringComparison.Ordinal))
            return path;

        // Bundled editor's choice sounds — resolved via BundledSounds
        if (path.StartsWith("bundled:", StringComparison.Ordinal))
            return path;

        // Normalize to canonical form (resolves / vs \ on Windows, .., etc.)
        string canonical;
        try
        {
            canonical = Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }

        // Must be an absolute path after normalization
        if (!Path.IsPathRooted(canonical))
            return null;

        return canonical;
    }

    private void MigrateToAlerts()
    {
        if (Alerts.Count == 0)
        {
            Alerts = CreateDefaultAlerts();

            // Carry forward old settings into the default alerts
            if (Alerts.Count >= 2)
            {
                var fullAlert = Alerts[0];
                fullAlert.LowerBound = FullBatteryNotificationValue;
                fullAlert.UpperBound = 100;
                fullAlert.IsEnabled = FullBatteryNotification;
                fullAlert.Sound = FullBatteryNotificationMusic;

                var lowAlert = Alerts[1];
                lowAlert.LowerBound = 0;
                lowAlert.UpperBound = LowBatteryNotificationValue;
                lowAlert.IsEnabled = LowBatteryNotification;
                lowAlert.Sound = LowBatteryNotificationMusic;
            }
        }

        SettingsVersion = 2;
        Save();
    }

    public static List<BatteryAlert> CreateDefaultAlerts() =>
    [
        new BatteryAlert
        {
            Id = "fullbatt",
            Label = "Full Battery",
            LowerBound = 80,
            UpperBound = 100,
            IsEnabled = true,
            Sound = "builtin:Harp"
        },
        new BatteryAlert
        {
            Id = "lowbatt_",
            Label = "Low Battery",
            LowerBound = 0,
            UpperBound = 25,
            IsEnabled = true,
            Sound = "builtin:Klaxon"
        }
    ];

    public void Reset()
    {
        FullBatteryNotification = true;
        LowBatteryNotification = true;
        FullBatteryNotificationValue = 96;
        LowBatteryNotificationValue = 25;
        FullBatteryNotificationMusic = "builtin:Harp";
        LowBatteryNotificationMusic = "builtin:Klaxon";
        StartMinimized = true;
        WindowPositionX = null;
        WindowPositionY = null;
        ThemeMode = ThemeMode.System;
        LaunchAtStartup = true;
        AutoCheckForUpdates = true;
        ScreenFlashEnabled = true;
        Alerts = CreateDefaultAlerts();
        SettingsVersion = 2;
        // AppId intentionally preserved — unique per install

        Save();
    }
}

public enum ThemeMode
{
    System,
    Light,
    Dark
}

public enum NotificationPosition
{
    TopLeft,
    TopCenter,
    TopRight,
    BottomLeft,
    BottomCenter,
    BottomRight
}

[JsonSerializable(typeof(AppSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal partial class AppSettingsJsonContext : JsonSerializerContext;
