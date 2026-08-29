using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    public partial class LowStockForm : Form
    {
        private DataGridView dgv;
        private Label lblCount;

        public LowStockForm()
        {
            this.Text = "Low Stock Report";
            this.Size = new Size(1020, 640);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this, false);
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F5) { LoadData(); e.Handled = true; } };

            this.Controls.Add(UiHelper.CreateFormBanner(
                "LOW STOCK",
                "At or below minimum  ·  F5 refresh  ·  Export CSV",
                FormAccent.Reports, FormAccent.ReportsDark));

            Panel top = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White };
            lblCount = new Label
            {
                Font = UiHelper.HeaderFont,
                ForeColor = UiHelper.DangerColor,
                AutoSize = true,
                Location = new Point(16, 16)
            };
            top.Controls.Add(lblCount);
            Button btnRefresh = new Button { Text = "Refresh (F5)", Location = new Point(320, 10), Size = new Size(120, 32) };
            UiHelper.StyleAccentButton(btnRefresh, FormAccent.Reports, FormAccent.ReportsDark);
            btnRefresh.Click += (s, e) => LoadData();
            top.Controls.Add(btnRefresh);
            Button btnExport = new Button { Text = "EXPORT CSV", Location = new Point(450, 10), Size = new Size(120, 32) };
            UiHelper.StyleAccentButton(btnExport, FormAccent.ReportsDark, FormAccent.Reports);
            btnExport.Click += (s, e) => CsvExport.FromGrid(dgv, "LowStock_" + DateTime.Today.ToString("yyyyMMdd") + ".csv");
            top.Controls.Add(btnExport);
            this.Controls.Add(top);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.Reports);
            this.Controls.Add(dgv);
            dgv.BringToFront();
            LoadData();
        }

        private void LoadData()
        {
            var list = new ProductRepository().GetLowStock();
            dgv.DataSource = null;
            dgv.DataSource = list;
            foreach (var h in new[] { "ProductID", "CategoryID", "IsActive", "CreatedDate" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["CurrentStock"].Value == null) continue;
                int stock = Convert.ToInt32(row.Cells["CurrentStock"].Value);
                if (stock <= 0)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 205, 210);
                else
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
            }
            if (lblCount != null)
                lblCount.Text = "Items at or below minimum:  " + list.Count;
        }
    }
}
