using System;
using System.Threading;

namespace BatteryNotifier.Core.Utils
{
    public sealed class Debouncer : IDisposable
    {
        private Timer? _timer;
        private Action? _taskToRun;
        private readonly object _lock = new();
        private bool _disposed;

        public void Debounce(Action task, int interval = Constants.DefaultNotificationTimeout)
        {
            if (interval < 0)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be non-negative");

            if (task == null)
                throw new ArgumentNullException(nameof(task));

            lock (_lock)
            {
                if (_disposed) return;

                _taskToRun = task;
                _timer?.Dispose();

                // Create a new timer that fires once after the interval
                _timer = new Timer(_ =>
                {
                    Action? action;
                    lock (_lock)
                    {
                        if (_disposed) return;
                        action = _taskToRun;
                        _taskToRun = null;
                    }

                    action?.Invoke();
                }, null, interval, Timeout.Infinite);
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _disposed = true;
                _timer?.Dispose();
                _timer = null;
                _taskToRun = null;
            }
        }
    }
}