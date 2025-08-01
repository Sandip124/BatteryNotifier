using System;
using System.Windows.Forms;
using BatteryNotifier.Constants;

namespace BatteryNotifier.Utils
{
    internal class Debouncer : IDisposable
    {
        private readonly Timer timer;
        private Action? taskToRun;

        public Debouncer()
        {
            timer = new Timer();
            timer.Tick += Timer_Tick;
        }

        public void Debounce(Action task, int interval = Constant.DefaultNotificationTimeout)
        {
            if (interval < 0) throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be non-negative");

            taskToRun = task ?? throw new ArgumentNullException(nameof(task));
            timer.Interval = interval;
            timer.Stop();
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            timer.Stop();
            var action = taskToRun;
            taskToRun = null;
            action?.Invoke();
        }

        public void Dispose()
        {
            timer.Tick -= Timer_Tick;
            timer.Dispose();
        }
    }
}