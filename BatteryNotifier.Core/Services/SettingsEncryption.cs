using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace BatteryNotifier.Core.Services;

/// <summary>
/// AES-256-GCM encryption for settings at rest.
/// Key is stored in a separate file with restrictive OS permissions.
///
/// File format: [12-byte nonce][16-byte tag][ciphertext]
/// </summary>
internal static class SettingsEncryption
{
    private const string KeyFileName = ".settings.key";
    private const int KeySizeBytes = 32;  // AES-256
    private const int NonceSizeBytes = 12; // GCM standard
    private const int TagSizeBytes = 16;   // GCM standard

    /// <summary>
    /// Encrypts plaintext JSON to a byte array (nonce + tag + ciphertext).
    /// </summary>
    public static byte[] Encrypt(string json, string settingsDirectory)
    {
        var key = GetOrCreateKey(settingsDirectory);
        var plaintext = Encoding.UTF8.GetBytes(json);

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
    }

    /// <summary>
    /// Decrypts a byte array back to plaintext JSON.
    /// Throws CryptographicException if tampered.
    /// </summary>
    public static string Decrypt(byte[] data, string settingsDirectory)
    {
        if (data.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Data too short to be a valid encrypted settings file.");

        var key = GetOrCreateKey(settingsDirectory);

        var nonce = data.AsSpan(0, NonceSizeBytes);
        var tag = data.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = data.AsSpan(NonceSizeBytes + TagSizeBytes);

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
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

    private static byte[] GetOrCreateKey(string settingsDirectory)
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
            return; // Windows NTFS inherits user-profile ACLs — sufficient for desktop app

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
