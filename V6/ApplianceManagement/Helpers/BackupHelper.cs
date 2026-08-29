using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Simple SQL Server BACKUP DATABASE via current connection string.
    /// Requires backup path writable by the SQL Server service account.
    /// </summary>
    public static class BackupHelper
    {
        public static void BackupInteractive(IWin32Window owner)
        {
            string cs = DbHelper.ConnectionString;
            if (string.IsNullOrWhiteSpace(cs))
            {
                DialogHelpers.Error(owner, "Connection string is empty.");
                return;
            }

            string dbName = null;
            try
            {
                var b = new SqlConnectionStringBuilder(cs);
                dbName = b.InitialCatalog;
            }
            catch
            {
                DialogHelpers.Error(owner, "Invalid connection string.");
                return;
            }

            if (string.IsNullOrWhiteSpace(dbName))
            {
                DialogHelpers.Error(owner, "Database name not found in connection string.");
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "SQL Backup (*.bak)|*.bak";
                dlg.FileName = dbName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".bak";
                dlg.Title = "Save database backup";
                if (dlg.ShowDialog(owner) != DialogResult.OK) return;

                string path = dlg.FileName;
                try
                {
                    // Prefer a folder SQL Server can write to; still try user path.
                    using (var conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        string sql = "BACKUP DATABASE [" + dbName.Replace("]", "]]") + "] TO DISK = @P WITH INIT";
                        using (var cmd = DbHelper.CreateCommand(sql, conn))
                        {
                            cmd.CommandTimeout = 600;
                            cmd.Parameters.AddWithValue("@P", path);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    DialogHelpers.Info(owner, "Backup completed:\n" + path +
                        "\n\nNote: SQL Server service must have write permission on this folder.");
                }
                catch (Exception ex)
                {
                    DialogHelpers.Error(owner,
                        "Backup failed.\n" + ex.Message +
                        "\n\nTip: Choose a path under SQL Server data folder, or grant the SQL service account write access.");
                }
            }
        }
    }
}
