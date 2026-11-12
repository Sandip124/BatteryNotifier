namespace BatteryNotifier.Core.Utils;

/// <summary>
/// Shared file-safety checks for paths that trace back to user input or settings (custom sound
/// imports, resolved playback paths, settings values) — canonicalize safely, reject symlinks,
/// reject oversized files. See CLAUDE.md "Defence-in-Depth for Sound Files".
/// </summary>
public static class FileSafety
{
    public const long DefaultMaxSizeBytes = 50 * 1024 * 1024;

    /// <summary>Canonicalizes a path and confirms the result is rooted. False on any failure.</summary>
    public static bool TryCanonicalize(string path, out string canonical)
    {
        canonical = string.Empty;
        try
        {
            var resolved = Path.GetFullPath(path);
            if (!Path.IsPathRooted(resolved)) return false;

            canonical = resolved;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>True if the file is a symlink — rejected so a resolved path can't be redirected outside the expected location.</summary>
    public static bool IsSymlink(FileInfo fileInfo) => fileInfo.LinkTarget != null;

    /// <summary>True if the file exceeds the given max size (default 50 MB).</summary>
    public static bool ExceedsMaxSize(FileInfo fileInfo, long maxSizeBytes = DefaultMaxSizeBytes) =>
        fileInfo.Length > maxSizeBytes;
}
