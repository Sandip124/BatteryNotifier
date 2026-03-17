using BatteryNotifier.Core.Utils;

namespace BatteryNotifier.Tests;

public class DebouncerTests
{
    [Fact]
    public async Task Debounce_RapidCalls_ExecutesOnlyLast()
    {
        using var debouncer = new Debouncer();
        int executedValue = 0;

        debouncer.Debounce(() => executedValue = 1, 100);
        debouncer.Debounce(() => executedValue = 2, 100);
        debouncer.Debounce(() => executedValue = 3, 100);

        await Task.Delay(500);

        Assert.Equal(3, executedValue);
    }

    [Fact]
    public async Task Debounce_FiresAfterInterval()
    {
        using var debouncer = new Debouncer();
        bool executed = false;

        debouncer.Debounce(() => executed = true, 50);

        Assert.False(executed);
        await Task.Delay(500);
        Assert.True(executed);
    }

    [Fact]
    public async Task Dispose_CancelsPendingAction()
    {
        var debouncer = new Debouncer();
        bool executed = false;

        debouncer.Debounce(() => executed = true, 100);
        debouncer.Dispose();

        await Task.Delay(500);

        Assert.False(executed);
    }

}
