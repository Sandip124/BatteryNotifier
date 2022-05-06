using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatteryNotifier.Helpers
{
    internal class ProcessHelper
    {
        public static bool IsAlreadyRunning()
        {
            return Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1;
        }
    }
}
