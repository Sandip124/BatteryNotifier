using System;
using System.IO;

namespace BatteryNotifier.Logger
{
    public static class Logger
    {
        private static StreamWriter swLog;
        private const string sLOG_FILE_PATH = "log.txt";

        static Logger() => OpenLogger();

        public static void OpenLogger()
        {
            swLog = new StreamWriter(sLOG_FILE_PATH, false);
            swLog.AutoFlush = true;
        }

        public static void LogThisLine(string sLogLine)
        {
            swLog.WriteLine(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "\t:" + "\t" + sLogLine);
            swLog.Flush();
        }

        public static void CloseLogger()
        {
            swLog.Flush();
            swLog.Close();
        }
    }
}
