using System.Security.Cryptography;
using System.Text.Json;

namespace BatteryNotifier.Core.Services;

public sealed class AppSettings
{
    private static readonly Lazy<AppSettings> _instance = new(() => new AppSettings());
    public static AppSettings Instance => _instance.Value;

    private const string SettingsFileName = "appsettings.json";
    private readonly object _saveLock = new();
    private string SettingsFilePath => Path.Combine(GetSettingsDirectory(), SettingsFileName);

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

    // App Identity
    public string AppId { get; set; } = Guid.NewGuid().ToString();

    private AppSettings()
    {
        Load();
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
                Save(); // Create default settings
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

            var settings = JsonSerializer.Deserialize<AppSettings>(json);

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
                AppId = settings.AppId;
            }

            // Re-save to encrypt if it was plaintext (migration)
            if (SettingsEncryption.IsPlaintext(rawBytes))
            {
                Save();
            }
        }
        catch (CryptographicException)
        {
            Reset();
        }
        catch (Exception)
        {
            Reset();
        }
    }

    public void Save()
    {
        lock (_saveLock)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(this, options);
                var encrypted = SettingsEncryption.Encrypt(json, GetSettingsDirectory());

                var tmpPath = SettingsFilePath + ".tmp";
                File.WriteAllBytes(tmpPath, encrypted);
                File.Move(tmpPath, SettingsFilePath, overwrite: true);
            }
            catch (Exception)
            {
                // Fail silently — settings won't persist
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
