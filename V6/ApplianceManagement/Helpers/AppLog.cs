using System;
using System.IO;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Minimal file logger for diagnostics (enterprise baseline).
    /// </summary>
    public static class AppLog
    {
        private static readonly object Sync = new object();
        private static readonly string LogDir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "logs");

        public static void Info(string message) => Write("INFO", message, null);
        public static void Warn(string message) => Write("WARN", message, null);
        public static void Error(string message, Exception ex = null) => Write("ERROR", message, ex);

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                if (!Directory.Exists(LogDir))
                    Directory.CreateDirectory(LogDir);

                string path = Path.Combine(LogDir, "app-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + level + "] " + message;
                if (ex != null)
                    line += Environment.NewLine + ex;

                lock (Sync)
                {
                    File.AppendAllText(path, line + Environment.NewLine);
                }
            }
            catch
            {
                // Never throw from logger
            }
        }
    }
}
