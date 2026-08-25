using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    public partial class InventoryForm : Form
    {
        private ProductRepository repo = new ProductRepository();
        private DataGridView dgv;
        private TextBox txtSearch;
        private Label lblCount;

        public InventoryForm()
        {
            InitializeComponent();
            LoadData("");
            txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "Stock Position";
            this.Size = new Size(1020, 640);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            this.Padding = new Padding(12);
            UiHelper.AttachF4Close(this);

            Panel top = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White, Padding = new Padding(16, 12, 16, 8) };
            Label lbl = new Label { Text = "Search", Font = UiHelper.NormalFont, AutoSize = true, Location = new Point(16, 16) };
            txtSearch = new TextBox { Location = new Point(80, 12), Size = new Size(340, 28), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadData(txtSearch.Text);
            lblCount = new Label { Font = UiHelper.SmallFont, ForeColor = Color.FromArgb(110, 122, 136), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            top.Controls.AddRange(new Control[] { lbl, txtSearch, lblCount });
            top.Resize += (s, e) =>
            {
                txtSearch.Width = Math.Max(200, top.Width - 280);
                lblCount.Location = new Point(top.Width - 16 - lblCount.Width, 16);
            };
            this.Controls.Add(top);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGrid(dgv);
            this.Controls.Add(dgv);
            dgv.BringToFront();
        }

        private void LoadData(string kw)
        {
            var list = string.IsNullOrWhiteSpace(kw) ? repo.GetAllActive() : repo.Search(kw);
            dgv.DataSource = null;
            dgv.DataSource = list;
            foreach (var h in new[] { "ProductID", "CategoryID", "IsActive", "CreatedDate" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
            if (dgv.Columns.Contains("StockValue")) dgv.Columns["StockValue"].HeaderText = "Stock Value";
            int low = 0;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["CurrentStock"].Value != null && row.Cells["MinimumStock"].Value != null)
                {
                    int stock = Convert.ToInt32(row.Cells["CurrentStock"].Value);
                    int min = Convert.ToInt32(row.Cells["MinimumStock"].Value);
                    if (stock <= min)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 238);
                        low++;
                    }
                }
            }
            if (lblCount != null) lblCount.Text = list.Count + " products   ·   " + low + " below minimum";
        }
    }
}
