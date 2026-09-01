using System;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Builds sale/purchase invoice numbers.
    /// If prefix is empty → plain number starting at 1 ("1", "2", …).
    /// If prefix set → prefix + zero-padded (e.g. INV-000001).
    /// </summary>
    public static class InvoiceNumberHelper
    {
        public static string Format(string prefix, int number)
        {
            if (number < 1) number = 1;
            if (string.IsNullOrWhiteSpace(prefix))
                return number.ToString();
            return prefix.Trim() + number.ToString("D6");
        }

        /// <summary>
        /// Priority: Settings table → license.dat InvoicePrefix → empty (no forced INV-).
        /// </summary>
        public static string ResolveSalePrefix(SqlConnection conn, SqlTransaction trans)
        {
            string fromDb = ReadSetting(conn, trans, "InvoicePrefix");
            if (!string.IsNullOrWhiteSpace(fromDb))
                return fromDb.Trim();

            if (LicenseReader.Current != null &&
                !string.IsNullOrWhiteSpace(LicenseReader.Current.InvoicePrefix))
                return LicenseReader.Current.InvoicePrefix.Trim();

            return ""; // no prefix → numbers 1, 2, 3…
        }

        public static string ResolvePurchasePrefix(SqlConnection conn, SqlTransaction trans)
        {
            string fromDb = ReadSetting(conn, trans, "PurchaseInvoicePrefix");
            if (!string.IsNullOrWhiteSpace(fromDb))
                return fromDb.Trim();
            return "";
        }

        public static string ResolveReturnPrefix(SqlConnection conn, SqlTransaction trans, string settingName)
        {
            string fromDb = ReadSetting(conn, trans, settingName);
            if (!string.IsNullOrWhiteSpace(fromDb))
                return fromDb.Trim();
            return "";
        }

        private static string ReadSetting(SqlConnection conn, SqlTransaction trans, string name)
        {
            using (var cmd = DbHelper.CreateCommand(
                "SELECT SettingValue FROM Settings WHERE SettingName=@N", conn, trans))
            {
                cmd.Parameters.AddWithValue("@N", name);
                var r = cmd.ExecuteScalar();
                if (r == null || r == DBNull.Value) return null;
                return r.ToString();
            }
        }

        public static int NextCounter(SqlConnection conn, SqlTransaction trans, string settingName)
        {
            using (var cmd = DbHelper.CreateCommand(
                "IF NOT EXISTS (SELECT 1 FROM Settings WITH (UPDLOCK, HOLDLOCK) WHERE SettingName=@N) " +
                "INSERT INTO Settings(SettingName,SettingValue) VALUES(@N,'1'); " +
                "UPDATE Settings WITH (UPDLOCK, ROWLOCK) SET SettingValue = CAST(CAST(ISNULL(NULLIF(SettingValue,''),'0') AS INT) + 1 AS NVARCHAR(50)) " +
                "WHERE SettingName=@N; " +
                "SELECT CAST(SettingValue AS INT) - 1 FROM Settings WHERE SettingName=@N;", conn, trans))
            {
                cmd.Parameters.AddWithValue("@N", settingName);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
