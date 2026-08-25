using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Authenticator.Forms
{
    public class AdminSaleModifyForm : Form
    {
        private string connStr;
        private DataGridView dgv;
        private TextBox txtPaid, txtRemarks;
        private int selectedId = 0;
        private decimal netAmt = 0;

        public AdminSaleModifyForm(string connectionString)
        {
            connStr = connectionString;
            Text = "Modify Sale";
            Size = new Size(880, 500);
            StartPosition = FormStartPosition.CenterParent;

            dgv = new DataGridView { Location = new Point(10, 10), Size = new Size(850, 340), ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                selectedId = Convert.ToInt32(dgv.CurrentRow.Cells["SaleID"].Value);
                netAmt = Convert.ToDecimal(dgv.CurrentRow.Cells["NetAmount"].Value);
                txtPaid.Text = Convert.ToString(dgv.CurrentRow.Cells["PaidAmount"].Value);
                txtRemarks.Text = Convert.ToString(dgv.CurrentRow.Cells["Remarks"].Value);
            };
            Controls.Add(dgv);
            Controls.Add(new Label { Text = "Paid:", Location = new Point(10, 370), Size = new Size(50, 22) });
            txtPaid = new TextBox { Location = new Point(70, 368), Size = new Size(120, 24) }; Controls.Add(txtPaid);
            Controls.Add(new Label { Text = "Remarks:", Location = new Point(210, 370), Size = new Size(70, 22) });
            txtRemarks = new TextBox { Location = new Point(290, 368), Size = new Size(300, 24) }; Controls.Add(txtRemarks);
            Button btn = new Button { Text = "Update", Location = new Point(610, 365), Size = new Size(120, 32), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btn.Click += (s, e) =>
            {
                if (selectedId == 0) return;
                decimal paid = 0; decimal.TryParse(txtPaid.Text, out paid);
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var cmd = new SqlCommand("UPDATE SaleHeader SET PaidAmount=@P, BalanceAmount=@B, Remarks=@R WHERE SaleID=@ID", c))
                    {
                        cmd.Parameters.AddWithValue("@P", paid);
                        cmd.Parameters.AddWithValue("@B", netAmt - paid);
                        cmd.Parameters.AddWithValue("@R", (object)txtRemarks.Text ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ID", selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Updated.");
                LoadGrid();
            };
            Controls.Add(btn);
            LoadGrid();
        }

        private void LoadGrid()
        {
            using (var c = new SqlConnection(connStr))
            using (var da = new SqlDataAdapter("SELECT TOP 200 SaleID,InvoiceNo,SaleDate,TotalAmount,Discount,NetAmount,PaidAmount,BalanceAmount,Remarks FROM SaleHeader ORDER BY SaleDate DESC", c))
            {
                var dt = new DataTable(); da.Fill(dt); dgv.DataSource = dt;
            }
        }
    }
}
