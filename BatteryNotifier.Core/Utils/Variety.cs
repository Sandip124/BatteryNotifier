namespace BatteryNotifier.Core.Utils;

/// <summary>
/// Rotating picker for message/phrase variety. Cycles through items (no immediate repeats) and
/// avoids System.Random entirely — variety here isn't security-sensitive. Thread-safe.
/// </summary>
public static class Variety
{
    private static int _rotation;

    public static int NextIndex(int count) =>
        count <= 1 ? 0 : (int)((uint)Interlocked.Increment(ref _rotation) % (uint)count);

    public static T Pick<T>(IReadOnlyList<T> items) => items[NextIndex(items.Count)];
}
