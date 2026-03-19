namespace BatteryNotifier.Core.Managers;

/// <summary>
/// Manages a library of user-imported custom sound files stored in the app's data directory.
/// Sound files are copied on import so they persist regardless of the original file's location.
/// Settings format: "custom:filename.wav" (filename only, resolved at runtime).
/// </summary>
public static class CustomSoundsLibrary
{
    public const string Prefix = "custom:";

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".wav", ".mp3", ".m4a", ".wma", ".ogg", ".flac", ".aac" };

    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    private static readonly string SoundsDir = Path.Combine(
        Constants.AppDataDirectory, "sounds");

    public static bool IsCustom(string? value) =>
        value != null && value.StartsWith(Prefix, StringComparison.Ordinal);

    public static string? GetFileName(string? value) =>
        IsCustom(value) ? value![Prefix.Length..] : null;

    public static string ToSettingsValue(string fileName) => Prefix + fileName;

    /// <summary>
    /// Resolves a "custom:filename.wav" settings value to a full file path.
    /// Returns null if the file doesn't exist (deleted externally).
    /// </summary>
    public static string? Resolve(string? settingsValue)
    {
        var fileName = GetFileName(settingsValue);
        if (string.IsNullOrEmpty(fileName))
            return null;

        // Prevent path traversal — only allow simple filenames
        if (fileName.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            fileName.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal) ||
            fileName.Contains('\0', StringComparison.Ordinal))
            return null;

        var path = Path.Combine(SoundsDir, fileName);
        return File.Exists(path) ? path : null;
    }

    /// <summary>
    /// Imports a sound file into the library by copying it to the sounds directory.
    /// Returns the filename used (may differ from source if collision occurred), or null on failure.
    /// </summary>
    public static string? Import(string sourcePath)
    {
        if (!ValidateSourceFile(sourcePath))
            return null;

        Directory.CreateDirectory(SoundsDir);

        var originalName = Path.GetFileName(sourcePath);
        if (string.IsNullOrEmpty(originalName))
            return null;

        var targetName = GetUniqueFileName(originalName);
        var targetPath = Path.Combine(SoundsDir, targetName);

        // Atomic copy: write to .tmp then rename
        var tmpPath = targetPath + ".tmp";
        try
        {
            File.Copy(sourcePath, tmpPath, overwrite: true);
            File.Move(tmpPath, targetPath, overwrite: true);
            return targetName;
        }
        catch
        {
            File.Delete(tmpPath);
            return null;
        }
    }

    /// <summary>Returns filenames of all imported custom sounds.</summary>
    public static IReadOnlyList<string> ListAll()
    {
        if (!Directory.Exists(SoundsDir))
            return [];

        var files = new List<string>();
        foreach (var path in Directory.EnumerateFiles(SoundsDir))
        {
            var ext = Path.GetExtension(path);
            if (AllowedExtensions.Contains(ext))
                files.Add(Path.GetFileName(path));
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);
        return files;
    }

    /// <summary>Deletes an imported sound by filename. Returns true if deleted.</summary>
    public static bool Delete(string fileName)
    {
        if (string.IsNullOrEmpty(fileName) ||
            fileName.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            fileName.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            return false;

        var path = Path.Combine(SoundsDir, fileName);
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;

    }

    private static bool ValidateSourceFile(string path)
    {
        if (!Path.IsPathRooted(path))
            return false;

        string canonical;
        try { canonical = Path.GetFullPath(path); }
        catch { return false; }

        var ext = Path.GetExtension(canonical);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return false;

        try
        {
            var info = new FileInfo(canonical);
            if (!info.Exists || info.Length > MaxFileSizeBytes)
                return false;
            if (info.LinkTarget != null)
                return false;
        }
        catch { return false; }

        return true;
    }

    private static string GetUniqueFileName(string name)
    {
        var targetPath = Path.Combine(SoundsDir, name);
        if (!File.Exists(targetPath))
            return name;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(name);
        var ext = Path.GetExtension(name);
        for (int i = 2; i < 1000; i++)
        {
            var candidate = $"{nameWithoutExt}_{i}{ext}";
            if (!File.Exists(Path.Combine(SoundsDir, candidate)))
                return candidate;
        }

        return $"{nameWithoutExt}_{Guid.NewGuid():N}{ext}";
    }
}
