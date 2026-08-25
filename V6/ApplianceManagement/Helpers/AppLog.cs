using System;
using System.IO;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Minimal file logger for diagnostics (enterprise baseline).
    /// Compatible with C# 7.3 / .NET Framework 4.7.2 (no expression-bodied statics relying on newer features).
    /// </summary>
    public static class AppLog
    {
        private static readonly object Sync = new object();

        private static string LogDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs"); }
        }

        public static void Info(string message)
        {
            Write("INFO", message, null);
        }

        public static void Warn(string message)
        {
            Write("WARN", message, null);
        }

        public static void Error(string message)
        {
            Write("ERROR", message, null);
        }

        public static void Error(string message, Exception ex)
        {
            Write("ERROR", message, ex);
        }

        private static void Write(string level, string message, Exception ex)
        {
            try
            {
                string dir = LogDir;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string path = Path.Combine(dir, "app-" + DateTime.Now.ToString("yyyyMMdd") + ".log");
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + level + "] " + message;
                if (ex != null)
                    line += Environment.NewLine + ex.ToString();

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
