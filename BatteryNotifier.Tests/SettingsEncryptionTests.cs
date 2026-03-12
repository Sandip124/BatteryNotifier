using System.Security.Cryptography;
using System.Text;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class SettingsEncryptionTests : IDisposable
{
    private readonly string _tempDir;

    public SettingsEncryptionTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "BatteryNotifierTest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void EncryptThenDecrypt_RoundTrip_PreservesContent()
    {
        var original = """{"FullBatteryNotification":true,"LowBatteryNotificationValue":25}""";

        var encrypted = SettingsEncryption.Encrypt(original, _tempDir);
        var decrypted = SettingsEncryption.Decrypt(encrypted, _tempDir);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentOutputEachTime()
    {
        // Due to random nonce, same plaintext should produce different ciphertext
        var json = """{"test":true}""";

        var encrypted1 = SettingsEncryption.Encrypt(json, _tempDir);
        var encrypted2 = SettingsEncryption.Encrypt(json, _tempDir);

        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ThrowsCryptographicException()
    {
        var json = """{"test":true}""";
        var encrypted = SettingsEncryption.Encrypt(json, _tempDir);

        // Tamper with the ciphertext (last byte)
        encrypted[^1] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() =>
            SettingsEncryption.Decrypt(encrypted, _tempDir));
    }

    [Fact]
    public void Decrypt_TamperedTag_ThrowsCryptographicException()
    {
        var json = """{"test":true}""";
        var encrypted = SettingsEncryption.Encrypt(json, _tempDir);

        // Tamper with the authentication tag (byte 13, which is in the tag region)
        encrypted[13] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() =>
            SettingsEncryption.Decrypt(encrypted, _tempDir));
    }

    [Fact]
    public void Decrypt_TamperedNonce_ThrowsCryptographicException()
    {
        var json = """{"test":true}""";
        var encrypted = SettingsEncryption.Encrypt(json, _tempDir);

        // Tamper with nonce (byte 0)
        encrypted[0] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() =>
            SettingsEncryption.Decrypt(encrypted, _tempDir));
    }

    [Fact]
    public void Decrypt_DataTooShort_ThrowsCryptographicException()
    {
        // Minimum is 12 (nonce) + 16 (tag) = 28 bytes
        var shortData = new byte[10];

        Assert.Throws<CryptographicException>(() =>
            SettingsEncryption.Decrypt(shortData, _tempDir));
    }

    [Fact]
    public void Decrypt_EmptyPlaintext_Works()
    {
        var encrypted = SettingsEncryption.Encrypt("", _tempDir);
        var decrypted = SettingsEncryption.Decrypt(encrypted, _tempDir);

        Assert.Equal("", decrypted);
    }

    [Fact]
    public void Encrypt_LargePayload_Works()
    {
        var largeJson = "{" + string.Join(",", Enumerable.Range(0, 1000).Select(i => $"\"key{i}\":{i}")) + "}";

        var encrypted = SettingsEncryption.Encrypt(largeJson, _tempDir);
        var decrypted = SettingsEncryption.Decrypt(encrypted, _tempDir);

        Assert.Equal(largeJson, decrypted);
    }

    [Fact]
    public void Encrypt_UnicodeContent_Works()
    {
        var unicode = """{"name":"テスト","emoji":"🔋","path":"C:\\Users\\用户"}""";

        var encrypted = SettingsEncryption.Encrypt(unicode, _tempDir);
        var decrypted = SettingsEncryption.Decrypt(encrypted, _tempDir);

        Assert.Equal(unicode, decrypted);
    }

    [Fact]
    public void GetOrCreateKey_CreatesKeyFile()
    {
        // Trigger key creation via encrypt
        SettingsEncryption.Encrypt("test", _tempDir);

        var keyPath = Path.Combine(_tempDir, ".settings.key");
        Assert.True(File.Exists(keyPath));

        var key = File.ReadAllBytes(keyPath);
        Assert.Equal(32, key.Length); // AES-256 key
    }

    [Fact]
    public void GetOrCreateKey_ReusesExistingKey()
    {
        // First encrypt creates the key
        SettingsEncryption.Encrypt("test1", _tempDir);
        var keyPath = Path.Combine(_tempDir, ".settings.key");
        var key1 = File.ReadAllBytes(keyPath);

        // Second encrypt should reuse the same key
        var encrypted = SettingsEncryption.Encrypt("test2", _tempDir);
        var key2 = File.ReadAllBytes(keyPath);

        Assert.Equal(key1, key2);
    }

    [Fact]
    public void Decrypt_WithDifferentKey_Fails()
    {
        var json = """{"test":true}""";
        var encrypted = SettingsEncryption.Encrypt(json, _tempDir);

        // Create a new temp dir with a different key
        var otherDir = Path.Combine(Path.GetTempPath(), "BatteryNotifierTest_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(otherDir);

        try
        {
            Assert.ThrowsAny<CryptographicException>(() =>
                SettingsEncryption.Decrypt(encrypted, otherDir));
        }
        finally
        {
            try { Directory.Delete(otherDir, recursive: true); } catch { }
        }
    }

    // ── IsPlaintext detection ────────────────────────────────────

    [Theory]
    [InlineData("{}", true)]
    [InlineData("  {}", true)]
    [InlineData("\t\n{}", true)]
    [InlineData("", false)]
    [InlineData("not json", false)]
    [InlineData("[1,2,3]", false)] // arrays don't start with '{'
    public void IsPlaintext_DetectsCorrectly(string content, bool expected)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        Assert.Equal(expected, SettingsEncryption.IsPlaintext(bytes));
    }

    [Fact]
    public void IsPlaintext_EmptyArray_ReturnsFalse()
    {
        Assert.False(SettingsEncryption.IsPlaintext([]));
    }

    [Fact]
    public void IsPlaintext_Utf8Bom_HandledCorrectly()
    {
        // UTF-8 BOM (EF BB BF) followed by '{'
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF, (byte)'{', (byte)'}' };
        Assert.True(SettingsEncryption.IsPlaintext(bytes));
    }

    [Fact]
    public void IsPlaintext_Utf8BomOnly_ReturnsFalse()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF };
        Assert.False(SettingsEncryption.IsPlaintext(bytes));
    }

    [Fact]
    public void IsPlaintext_EncryptedData_ReturnsFalse()
    {
        var encrypted = SettingsEncryption.Encrypt("""{"test":true}""", _tempDir);
        // Encrypted data should not look like plaintext
        // (statistically very unlikely that random bytes start with '{')
        Assert.False(SettingsEncryption.IsPlaintext(encrypted));
    }
}
