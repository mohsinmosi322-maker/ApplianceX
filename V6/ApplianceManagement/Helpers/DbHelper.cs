using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;

namespace ApplianceManagement.Helpers
{
    public static class DbHelper
    {
        /// <summary>
        /// Priority: license.dat → connectionstring.txt (exe folder) → App.config ApplianceDb.
        /// </summary>
        public static string ConnectionString
        {
            get
            {
                if (LicenseReader.Current != null &&
                    !string.IsNullOrWhiteSpace(LicenseReader.Current.ConnectionString))
                {
                    return LicenseReader.Current.ConnectionString.Trim();
                }

                string fromFile = TryReadConnectionStringFile();
                if (!string.IsNullOrWhiteSpace(fromFile))
                    return fromFile.Trim();

                var cs = ConfigurationManager.ConnectionStrings["ApplianceDb"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                    throw new ConfigurationErrorsException(
                        "No connection string found.\n\n" +
                        "Provide one of:\n" +
                        "  • license.dat (Authenticator)\n" +
                        "  • connectionstring.txt next to the .exe\n" +
                        "  • App.config key ApplianceDb");

                return cs.ConnectionString;
            }
        }

        private static string TryReadConnectionStringFile()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "connectionstring.txt");
                if (!File.Exists(path)) return null;
                string text = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(text)) return null;
                // first non-empty, non-comment line
                foreach (var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string t = line.Trim();
                    if (t.StartsWith("#") || t.StartsWith(";")) continue;
                    return t;
                }
            }
            catch (Exception ex)
            {
                AppLog.Warn("Could not read connectionstring.txt: " + ex.Message);
            }
            return null;
        }

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        public static SqlCommand CreateCommand(string sql, SqlConnection conn, SqlTransaction trans = null)
        {
            var cmd = new SqlCommand(sql, conn);
            if (trans != null) cmd.Transaction = trans;
            return cmd;
        }

        /// <summary>Quick connectivity check for login / setup.</summary>
        public static bool TryOpen(out string error)
        {
            error = null;
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
