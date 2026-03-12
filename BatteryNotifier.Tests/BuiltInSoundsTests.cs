using BatteryNotifier.Core.Managers;

namespace BatteryNotifier.Tests;

public class BuiltInSoundsTests
{
    [Theory]
    [InlineData("builtin:Chime")]
    [InlineData("builtin:Alert")]
    [InlineData("builtin:Gentle")]
    [InlineData("builtin:Ping")]
    [InlineData("builtin:Beacon")]
    public void IsBuiltIn_ValidPrefix_ReturnsTrue(string value)
    {
        Assert.True(BuiltInSounds.IsBuiltIn(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/path/to/file.wav")]
    [InlineData("Chime")]
    [InlineData("BUILTIN:Chime")] // case sensitive
    public void IsBuiltIn_InvalidPrefix_ReturnsFalse(string? value)
    {
        Assert.False(BuiltInSounds.IsBuiltIn(value));
    }

    [Theory]
    [InlineData("builtin:Chime", "Chime")]
    [InlineData("builtin:Alert", "Alert")]
    [InlineData("builtin:Gentle", "Gentle")]
    [InlineData("builtin:Ping", "Ping")]
    [InlineData("builtin:Beacon", "Beacon")]
    public void GetName_ExtractsCorrectName(string value, string expected)
    {
        Assert.Equal(expected, BuiltInSounds.GetName(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/some/path.wav")]
    public void GetName_NonBuiltIn_ReturnsNull(string? value)
    {
        Assert.Null(BuiltInSounds.GetName(value));
    }

    [Theory]
    [InlineData("Chime")]
    [InlineData("Alert")]
    public void ToSettingsValue_PrependPrefix(string name)
    {
        var result = BuiltInSounds.ToSettingsValue(name);
        Assert.Equal($"builtin:{name}", result);
        Assert.True(BuiltInSounds.IsBuiltIn(result));
    }

    [Fact]
    public void Names_ContainsAllBuiltInSounds()
    {
        var names = BuiltInSounds.Names;
        Assert.Contains("Chime", names);
        Assert.Contains("Alert", names);
        Assert.Contains("Gentle", names);
        Assert.Contains("Ping", names);
        Assert.Contains("Beacon", names);
        Assert.Equal(5, names.Count);
    }

    [Fact]
    public void Resolve_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(BuiltInSounds.Resolve(null));
        Assert.Null(BuiltInSounds.Resolve(""));
    }

    [Fact]
    public void Resolve_CustomPath_ReturnsAsIs()
    {
        var path = "/some/custom/sound.wav";
        Assert.Equal(path, BuiltInSounds.Resolve(path));
    }

    [Fact]
    public void Resolve_UnknownBuiltIn_ReturnsNull()
    {
        // Prevents path traversal via crafted names
        Assert.Null(BuiltInSounds.Resolve("builtin:../../etc/passwd"));
        Assert.Null(BuiltInSounds.Resolve("builtin:NonExistent"));
    }

    [Theory]
    [InlineData("builtin:Chime")]
    [InlineData("builtin:Alert")]
    [InlineData("builtin:Gentle")]
    [InlineData("builtin:Ping")]
    [InlineData("builtin:Beacon")]
    public void Resolve_ValidBuiltIn_ReturnsExistingWavFile(string settingsValue)
    {
        var path = BuiltInSounds.Resolve(settingsValue);

        Assert.NotNull(path);
        Assert.True(File.Exists(path), $"WAV file should exist at {path}");
        Assert.EndsWith(".wav", path);
    }

    [Theory]
    [InlineData("builtin:Chime")]
    [InlineData("builtin:Alert")]
    public void Resolve_CalledTwice_ReturnsSamePath(string settingsValue)
    {
        // Caching: second call should return same cached file
        var path1 = BuiltInSounds.Resolve(settingsValue);
        var path2 = BuiltInSounds.Resolve(settingsValue);

        Assert.Equal(path1, path2);
    }

    [Theory]
    [InlineData("builtin:Chime")]
    [InlineData("builtin:Alert")]
    [InlineData("builtin:Gentle")]
    [InlineData("builtin:Ping")]
    [InlineData("builtin:Beacon")]
    public void GeneratedWav_HasValidRiffHeader(string settingsValue)
    {
        var path = BuiltInSounds.Resolve(settingsValue)!;
        var bytes = File.ReadAllBytes(path);

        // RIFF header check
        Assert.True(bytes.Length > 44, "WAV file too small");
        Assert.Equal((byte)'R', bytes[0]);
        Assert.Equal((byte)'I', bytes[1]);
        Assert.Equal((byte)'F', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);

        // WAVE format
        Assert.Equal((byte)'W', bytes[8]);
        Assert.Equal((byte)'A', bytes[9]);
        Assert.Equal((byte)'V', bytes[10]);
        Assert.Equal((byte)'E', bytes[11]);
    }
}
