using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatteryNotifier.CustomTimer
{
    public partial class CustomTimer
    {
        public int TimerCount { get; private set; } = 0;
        public void ResetTimer() => TimerCount = 0;
        public void Increment() => TimerCount++;
    }
}
