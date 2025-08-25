using System;
using System.Threading;

namespace BatteryNotifier.Core.Utils
{
    public class Debouncer : IDisposable
    {
        private Timer? _timer;
        private Action? _taskToRun;
        private readonly object _lock = new();

        public void Debounce(Action task, int interval = Constants.DefaultNotificationTimeout)
        {
            if (interval < 0)
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be non-negative");

            if (task == null)
                throw new ArgumentNullException(nameof(task));

            lock (_lock)
            {
                _taskToRun = task;
                _timer?.Dispose();

                // Create a new timer that fires once after the interval
                _timer = new Timer(_ =>
                {
                    Action? action;
                    lock (_lock)
                    {
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
                _timer?.Dispose();
                _taskToRun = null;
            }
        }
    }
}