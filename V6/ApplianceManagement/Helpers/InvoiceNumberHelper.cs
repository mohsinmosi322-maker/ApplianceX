using System;
using System.Data.SqlClient;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Invoice numbers are plain integers: 1, 2, 3… (no INV-/PUR- prefix).
    /// Prefix settings and license InvoicePrefix are ignored by design.
    /// </summary>
    public static class InvoiceNumberHelper
    {
        public static string Format(string prefix, int number)
        {
            if (number < 1) number = 1;
            // Always plain number — user requirement: no prefix
            return number.ToString();
        }

        public static string ResolveSalePrefix(SqlConnection conn, SqlTransaction trans)
        {
            return "";
        }

        public static string ResolvePurchasePrefix(SqlConnection conn, SqlTransaction trans)
        {
            return "";
        }

        public static string ResolveReturnPrefix(SqlConnection conn, SqlTransaction trans, string settingName)
        {
            return "";
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
