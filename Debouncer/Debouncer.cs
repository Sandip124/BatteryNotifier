using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BatteryNotifier.Debouncer
{
    internal class Debouncer
    {
        private Timer? timer;
        public void Debounce(Action taskToRun, int interval)
        {
            if (timer != null)
            {
                ClearTimer();
            }

            timer = new Timer { Interval = interval };
            timer.Tick += (_, __) =>
            {
                ClearTimer();
                taskToRun();
            };
            timer.Start();

            void ClearTimer()
            {
                timer.Stop();
                timer = null;
            }
        }

    }
}
