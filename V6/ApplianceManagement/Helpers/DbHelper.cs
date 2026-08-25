using System.Configuration;
using System.Data.SqlClient;

namespace ApplianceManagement.Helpers
{
    public static class DbHelper
    {
        /// <summary>
        /// Prefer connection string from valid license; fall back to App.config.
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

                var cs = ConfigurationManager.ConnectionStrings["ApplianceDb"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                    throw new ConfigurationErrorsException(
                        "Connection string 'ApplianceDb' is missing in App.config and license.dat.");

                return cs.ConnectionString;
            }
        }

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        public static SqlCommand CreateCommand(string sql, SqlConnection conn, SqlTransaction trans = null)
        {
            var cmd = new SqlCommand(sql, conn);
            if (trans != null) cmd.Transaction = trans;
            return cmd;
        }
    }
}
