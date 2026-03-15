using BatteryNotifier.Core.Services;

namespace BatteryNotifier.Core.Managers;

/// <summary>
/// Manages in-app inline notification state with auto-dismiss.
/// UI layers observe <see cref="StateChanged"/> to update bindings.
/// </summary>
public sealed class InlineNotificationManager : IDisposable
{
    private static readonly Lazy<InlineNotificationManager> _instance = new(() => new InlineNotificationManager());
    public static InlineNotificationManager Instance => _instance.Value;

    private CancellationTokenSource? _dismissCts;
    private readonly object _lock = new();
    private bool _disposed;

    public string Message { get; private set; } = string.Empty;
    public InlineNotificationLevel Level { get; private set; }
    public bool IsVisible { get; private set; }

    /// <summary>
    /// Raised on the thread that called Show/Dismiss. UI layers must marshal to their UI thread.
    /// </summary>
    public event Action? StateChanged;

    private InlineNotificationManager() { }

    public void Show(string message, InlineNotificationLevel level = InlineNotificationLevel.Info, int durationMs = 3000)
    {
        lock (_lock)
        {
            if (_disposed) return;

            _dismissCts?.Cancel();
            _dismissCts?.Dispose();

            Message = message;
            Level = level;
            IsVisible = true;

            var cts = new CancellationTokenSource();
            _dismissCts = cts;

            _ = AutoDismissAsync(durationMs, cts.Token);
        }

        StateChanged?.Invoke();
    }

    public void Dismiss()
    {
        lock (_lock)
        {
            _dismissCts?.Cancel();
            _dismissCts?.Dispose();
            _dismissCts = null;
            IsVisible = false;
        }

        StateChanged?.Invoke();
    }

    private async Task AutoDismissAsync(int durationMs, CancellationToken ct)
    {
        try
        {
            await Task.Delay(durationMs, ct).ConfigureAwait(false);

            lock (_lock)
            {
                if (ct.IsCancellationRequested) return;
                IsVisible = false;
            }

            StateChanged?.Invoke();
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _dismissCts?.Cancel();
            _dismissCts?.Dispose();
            _dismissCts = null;
        }

        StateChanged = null;
    }
}
