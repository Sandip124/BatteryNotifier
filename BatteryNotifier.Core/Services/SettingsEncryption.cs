using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// Platform-aware encryption for settings at rest.
///
/// Windows: Uses DPAPI (ProtectedData) — keys managed by the OS, tied to the
/// current user account. No key file on disk, no AV-suspicious crypto patterns.
///
/// macOS/Linux: Uses AES-256-GCM with a local key file (chmod 600).
///
/// AES-GCM file format: [12-byte nonce][16-byte tag][ciphertext]
/// DPAPI file format: opaque byte array managed by Windows.
///
/// Migration: On Windows, if DPAPI decryption fails, falls back to AES-GCM
/// to transparently migrate settings encrypted by previous versions. The legacy
/// key file is cleaned up after the first successful DPAPI encryption.
/// </summary>
internal static class SettingsEncryption
{
    private const string KeyFileName = ".settings.key";
    private const int KeySizeBytes = 32;  // AES-256
    private const int NonceSizeBytes = 12; // GCM standard
    private const int TagSizeBytes = 16;   // GCM standard

    /// <summary>
    /// Encrypts plaintext JSON to a byte array.
    /// </summary>
    public static byte[] Encrypt(string json, string settingsDirectory)
    {
        var plaintext = Encoding.UTF8.GetBytes(json);

#if WINDOWS
        // DPAPI: OS-managed encryption tied to the current user account.
        // No key file needed — eliminates the .settings.key file pattern
        // that can trigger AV heuristics (resembles ransomware key storage).
        var result = System.Security.Cryptography.ProtectedData.Protect(
            plaintext, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);

        // Clean up legacy AES key file after successful DPAPI encryption
        try
        {
            var keyPath = Path.Combine(settingsDirectory, KeyFileName);
            if (File.Exists(keyPath)) File.Delete(keyPath);
        }
        catch { /* best effort */ }

        return result;
#else
        // AES-256-GCM with local key file (macOS/Linux)
        var key = GetOrCreateKey(settingsDirectory);

        var nonce = new byte[NonceSizeBytes];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // Format: [nonce][tag][ciphertext]
        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSizeBytes);
        ciphertext.CopyTo(result, NonceSizeBytes + TagSizeBytes);

        return result;
#endif
    }

    /// <summary>
    /// Decrypts a byte array back to plaintext JSON.
    /// Throws CryptographicException if tampered.
    /// </summary>
    public static string Decrypt(byte[] data, string settingsDirectory)
    {
#if WINDOWS
        // Try DPAPI first (current format on Windows)
        try
        {
            var plaintext = System.Security.Cryptography.ProtectedData.Unprotect(
                data, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException) when (File.Exists(Path.Combine(settingsDirectory, KeyFileName)))
        {
            // DPAPI failed but legacy AES key exists — fall through to AES-GCM migration
        }
#endif

        // AES-GCM decryption (primary on macOS/Linux, migration fallback on Windows)
        if (data.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Data too short to be a valid encrypted settings file.");

        var key = GetOrCreateKey(settingsDirectory);

        var nonce = data.AsSpan(0, NonceSizeBytes);
        var tag = data.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = data.AsSpan(NonceSizeBytes + TagSizeBytes);

        var plaintext2 = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext2);

        return Encoding.UTF8.GetString(plaintext2);
    }

    /// <summary>
    /// Returns true if the file content looks like plaintext JSON (starts with '{').
    /// Used to detect and migrate legacy unencrypted settings.
    /// </summary>
    public static bool IsPlaintext(byte[] data)
    {
        if (data.Length == 0) return false;

        // Skip UTF-8 BOM if present
        int start = 0;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
            start = 3;

        // Skip whitespace
        while (start < data.Length && (data[start] == ' ' || data[start] == '\t' || data[start] == '\r' || data[start] == '\n'))
            start++;

        return start < data.Length && data[start] == (byte)'{';
    }

    // GetOrCreateKey and SetRestrictivePermissions are always available:
    // - Primary use on macOS/Linux (AES-GCM encryption)
    // - Migration fallback on Windows (reading legacy AES key)
    // - Tests (cross-platform)

    internal static byte[] GetOrCreateKey(string settingsDirectory)
    {
        var keyPath = Path.Combine(settingsDirectory, KeyFileName);

        if (File.Exists(keyPath))
        {
            var existing = File.ReadAllBytes(keyPath);
            if (existing.Length == KeySizeBytes)
                return existing;
        }

        // Generate new key
        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        File.WriteAllBytes(keyPath, key);
        SetRestrictivePermissions(keyPath);

        return key;
    }

    private static void SetRestrictivePermissions(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return; // Windows: DPAPI handles security; NTFS ACLs protect fallback key file

        // macOS / Linux: chmod 600 (owner read/write only)
        try
        {
            File.SetUnixFileMode(path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch
        {
            // Best effort — old .NET or unusual filesystem
        }
    }
}
