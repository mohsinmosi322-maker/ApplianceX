using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace Authenticator.Forms
{
    public class AdminProductForm : Form
    {
        private string connStr;
        private DataGridView dgv;
        private TextBox txtName, txtSale, txtPur, txtMin;
        private CheckBox chkActive;
        private int selectedId = 0;

        public AdminProductForm(string connectionString)
        {
            connStr = connectionString;
            Text = "Manage Products";
            Size = new Size(900, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 247, 250);

            dgv = new DataGridView { Location = new Point(10, 10), Size = new Size(560, 450), ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.CurrentRow == null) return;
                selectedId = Convert.ToInt32(dgv.CurrentRow.Cells["ProductID"].Value);
                txtName.Text = Convert.ToString(dgv.CurrentRow.Cells["ProductName"].Value);
                txtSale.Text = Convert.ToString(dgv.CurrentRow.Cells["SalePrice"].Value);
                txtPur.Text = Convert.ToString(dgv.CurrentRow.Cells["PurchasePrice"].Value);
                txtMin.Text = Convert.ToString(dgv.CurrentRow.Cells["MinimumStock"].Value);
                chkActive.Checked = Convert.ToBoolean(dgv.CurrentRow.Cells["IsActive"].Value);
            };
            Controls.Add(dgv);

            int y = 20;
            Controls.Add(new Label { Text = "Name:", Location = new Point(590, y), Size = new Size(80, 22) });
            txtName = new TextBox { Location = new Point(680, y), Size = new Size(180, 24) }; Controls.Add(txtName); y += 36;
            Controls.Add(new Label { Text = "Sale:", Location = new Point(590, y), Size = new Size(80, 22) });
            txtSale = new TextBox { Location = new Point(680, y), Size = new Size(100, 24) }; Controls.Add(txtSale); y += 36;
            Controls.Add(new Label { Text = "Purchase:", Location = new Point(590, y), Size = new Size(80, 22) });
            txtPur = new TextBox { Location = new Point(680, y), Size = new Size(100, 24) }; Controls.Add(txtPur); y += 36;
            Controls.Add(new Label { Text = "Min Stock:", Location = new Point(590, y), Size = new Size(80, 22) });
            txtMin = new TextBox { Location = new Point(680, y), Size = new Size(80, 24) }; Controls.Add(txtMin); y += 36;
            chkActive = new CheckBox { Text = "Active (for sale)", Location = new Point(590, y), Size = new Size(200, 24), Checked = true };
            Controls.Add(chkActive); y += 40;

            Button btnSave = new Button { Text = "Save", Location = new Point(590, y), Size = new Size(120, 32), BackColor = Color.FromArgb(41, 128, 185), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += (s, e) =>
            {
                if (selectedId == 0) return;
                decimal sale = 0, pur = 0; int min = 0;
                decimal.TryParse(txtSale.Text, out sale); decimal.TryParse(txtPur.Text, out pur); int.TryParse(txtMin.Text, out min);
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var cmd = new SqlCommand("UPDATE Products SET ProductName=@N,SalePrice=@S,PurchasePrice=@P,MinimumStock=@M,IsActive=@A WHERE ProductID=@ID", c))
                    {
                        cmd.Parameters.AddWithValue("@N", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@S", sale);
                        cmd.Parameters.AddWithValue("@P", pur);
                        cmd.Parameters.AddWithValue("@M", min);
                        cmd.Parameters.AddWithValue("@A", chkActive.Checked);
                        cmd.Parameters.AddWithValue("@ID", selectedId);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Updated.");
                LoadGrid();
            };
            Controls.Add(btnSave); y += 40;
            Button btnDis = new Button { Text = "Disable", Location = new Point(590, y), Size = new Size(120, 32), BackColor = Color.FromArgb(192, 57, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnDis.Click += (s, e) =>
            {
                if (selectedId == 0) return;
                using (var c = new SqlConnection(connStr))
                {
                    c.Open();
                    using (var cmd = new SqlCommand("UPDATE Products SET IsActive=0 WHERE ProductID=@ID", c))
                    { cmd.Parameters.AddWithValue("@ID", selectedId); cmd.ExecuteNonQuery(); }
                }
                MessageBox.Show("Disabled.");
                LoadGrid();
            };
            Controls.Add(btnDis);
            LoadGrid();
        }

        private void LoadGrid()
        {
            using (var c = new SqlConnection(connStr))
            using (var da = new SqlDataAdapter("SELECT ProductID,ProductCode,ProductName,PurchasePrice,SalePrice,MinimumStock,IsActive FROM Products ORDER BY ProductName", c))
            {
                var dt = new DataTable();
                da.Fill(dt);
                dgv.DataSource = dt;
            }
        }
    }
}
