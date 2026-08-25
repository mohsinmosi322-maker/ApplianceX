using System;
using System.Data.SqlClient;

namespace ApplianceManagement.Helpers
{
    public static class AppSettings
    {
        public static string Get(string name)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT SettingValue FROM Settings WHERE SettingName=@N", conn))
                {
                    cmd.Parameters.AddWithValue("@N", name);
                    var r = cmd.ExecuteScalar();
                    return r == null || r == DBNull.Value ? "" : r.ToString();
                }
            }
        }

        public static void Set(string name, string value)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                string sql = @"IF EXISTS(SELECT 1 FROM Settings WHERE SettingName=@N)
                               UPDATE Settings SET SettingValue=@V WHERE SettingName=@N
                               ELSE INSERT INTO Settings(SettingName,SettingValue) VALUES(@N,@V)";
                using (var cmd = DbHelper.CreateCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@N", name);
                    cmd.Parameters.AddWithValue("@V", (object)value ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static string GetUserPermissions(string userName)
        {
            string val = Get("Perm_" + userName);
            // Default: all except SETTINGS for normal users
            return string.IsNullOrEmpty(val) ? "SALE,PURCHASE,NEWITEM,INVENTORY,REPORTS" : val;
        }

        public static void SetUserPermissions(string userName, string perms)
        {
            Set("Perm_" + userName, perms);
        }

        public static bool HasPermission(string userName, string role, string menu)
        {
            if (role == "Admin") return true;
            string perms = GetUserPermissions(userName);
            return ("," + perms.ToUpper() + ",").Contains("," + menu.ToUpper() + ",");
        }
    }
}
