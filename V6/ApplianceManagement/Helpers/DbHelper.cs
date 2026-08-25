using System.Configuration;
using System.Data.SqlClient;

namespace ApplianceManagement.Helpers
{
    public static class DbHelper
    {
        private static readonly string ConnectionString =
            ConfigurationManager.ConnectionStrings["ApplianceDb"].ConnectionString;

        public static SqlConnection GetConnection() => new SqlConnection(ConnectionString);

        public static SqlCommand CreateCommand(string sql, SqlConnection conn, SqlTransaction trans = null)
        {
            var cmd = new SqlCommand(sql, conn);
            if (trans != null) cmd.Transaction = trans;
            return cmd;
        }
    }
}
