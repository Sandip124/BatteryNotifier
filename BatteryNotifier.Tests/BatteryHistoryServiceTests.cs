using System.Text.Json;
using BatteryNotifier.Core.Models;
using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Tests;

public class BatteryHistoryServiceTests
{
    [Fact]
    public void ChargeHistoryEntry_Roundtrip_Serialization()
    {
        var entries = new List<ChargeHistoryEntry>
        {
            new(1710000000, 85, true),
            new(1710000060, 86, true),
            new(1710000120, 87, false),
        };

        var json = JsonSerializer.Serialize(entries, BatteryHistoryJsonContext.Default.Options);
        var deserialized = JsonSerializer.Deserialize<List<ChargeHistoryEntry>>(json, BatteryHistoryJsonContext.Default.Options);

        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Count);
        Assert.Equal(85, deserialized[0].Percent);
        Assert.True(deserialized[0].IsCharging);
        Assert.Equal(1710000000, deserialized[0].TimestampUnixSeconds);
        Assert.False(deserialized[2].IsCharging);
    }

    [Fact]
    public void WearHistoryEntry_Roundtrip_Serialization()
    {
        var entries = new List<WearHistoryEntry>
        {
            new(1710000000, 98.5, 42),
            new(1710086400, 98.4, null),
        };

        var json = JsonSerializer.Serialize(entries, BatteryHistoryJsonContext.Default.Options);
        var deserialized = JsonSerializer.Deserialize<List<WearHistoryEntry>>(json, BatteryHistoryJsonContext.Default.Options);

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Count);
        Assert.Equal(98.5, deserialized[0].HealthPercent);
        Assert.Equal(42, deserialized[0].CycleCount);
        Assert.Null(deserialized[1].CycleCount);
    }

    [Fact]
    public void ChargeHistoryEntry_EmptyList_Serialization()
    {
        var entries = new List<ChargeHistoryEntry>();
        var json = JsonSerializer.Serialize(entries, BatteryHistoryJsonContext.Default.Options);
        var deserialized = JsonSerializer.Deserialize<List<ChargeHistoryEntry>>(json, BatteryHistoryJsonContext.Default.Options);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized);
    }

    [Fact]
    public void WearHistoryEntry_EmptyList_Serialization()
    {
        var entries = new List<WearHistoryEntry>();
        var json = JsonSerializer.Serialize(entries, BatteryHistoryJsonContext.Default.Options);
        var deserialized = JsonSerializer.Deserialize<List<WearHistoryEntry>>(json, BatteryHistoryJsonContext.Default.Options);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized);
    }

    [Fact]
    public void CorruptJson_Throws_JsonException()
    {
        // BatteryHistoryService catches this and starts fresh — verify it does throw
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<List<ChargeHistoryEntry>>(
                "not valid json{{{", BatteryHistoryJsonContext.Default.Options));
    }

    [Fact]
    public void ChargeHistoryEntry_BoundaryValues()
    {
        var entries = new List<ChargeHistoryEntry>
        {
            new(0, 0, false),
            new(long.MaxValue / 2, 100, true),
        };

        var json = JsonSerializer.Serialize(entries, BatteryHistoryJsonContext.Default.Options);
        var deserialized = JsonSerializer.Deserialize<List<ChargeHistoryEntry>>(json, BatteryHistoryJsonContext.Default.Options);

        Assert.NotNull(deserialized);
        Assert.Equal(0, deserialized[0].Percent);
        Assert.Equal(100, deserialized[1].Percent);
    }

    [Fact]
    public void WearHistoryEntry_HealthPercent_Precision()
    {
        var entry = new WearHistoryEntry(1710000000, 87.123456, 150);
        var entries = new List<WearHistoryEntry> { entry };

        var json = JsonSerializer.Serialize(entries, BatteryHistoryJsonContext.Default.Options);
        var deserialized = JsonSerializer.Deserialize<List<WearHistoryEntry>>(json, BatteryHistoryJsonContext.Default.Options);

        Assert.NotNull(deserialized);
        Assert.Equal(87.123456, deserialized[0].HealthPercent, 6);
    }
}
