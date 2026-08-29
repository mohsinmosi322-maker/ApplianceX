using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ApplianceManagement.Helpers
{
    public static class CsvExport
    {
        public static void FromGrid(DataGridView dgv, string defaultFileName)
        {
            if (dgv == null || dgv.Rows.Count == 0)
            {
                DialogHelpers.Info(null, "Nothing to export.");
                return;
            }

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "CSV files (*.csv)|*.csv";
                dlg.FileName = defaultFileName ?? "export.csv";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                var sb = new StringBuilder();
                // headers
                bool first = true;
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (!col.Visible) continue;
                    if (!first) sb.Append(',');
                    sb.Append(Escape(col.HeaderText));
                    first = false;
                }
                sb.AppendLine();

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;
                    first = true;
                    foreach (DataGridViewColumn col in dgv.Columns)
                    {
                        if (!col.Visible) continue;
                        if (!first) sb.Append(',');
                        object val = row.Cells[col.Index].Value;
                        sb.Append(Escape(val == null ? "" : val.ToString()));
                        first = false;
                    }
                    sb.AppendLine();
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                DialogHelpers.Info(null, "Exported to:\n" + dlg.FileName);
            }
        }

        private static string Escape(string s)
        {
            if (s == null) return "\"\"";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
