using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using BatteryNotifier.Forms;

namespace BatteryNotifier
{
    internal static class Program
    {
        private static string appGuid = "D2ED1949-C00C-4F99-87DD-B5A6CE56A733";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.

            using Mutex mutex = new Mutex(false, "Global\\" + appGuid);
            if (!mutex.WaitOne(0, false))
            {
                Process[] proc = Process.GetProcessesByName("BatteryNotifier");
                Interaction.AppActivate(proc[0].MainWindowTitle);
                return;
            }

            GC.Collect();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Dashboard());

        }
    }
}
