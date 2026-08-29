using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class InventoryForm : Form
    {
        private readonly ProductRepository repo = new ProductRepository();
        private DataGridView dgv;
        private TextBox txtSearch;
        private Label lblCount;
        private ComboBox cmbFilter;

        public InventoryForm()
        {
            InitializeComponent();
            LoadData();
            txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "Stock Position";
            this.Size = new Size(1020, 640);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this, false);
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F5) { LoadData(); e.Handled = true; } };

            this.Controls.Add(UiHelper.CreateFormBanner(
                "STOCK POSITION",
                "Ledger-based stock  ·  Filter: All / Low / Out  ·  F5 refresh  F4 close",
                FormAccent.Inventory, FormAccent.InventoryDark));

            Panel top = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White, Padding = new Padding(12, 10, 12, 8) };
            top.Controls.Add(new Label { Text = "Search", Font = UiHelper.NormalFont, AutoSize = true, Location = new Point(12, 16) });
            txtSearch = new TextBox { Location = new Point(70, 12), Size = new Size(280, 28) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadData();
            top.Controls.Add(txtSearch);

            top.Controls.Add(new Label { Text = "Filter", Font = UiHelper.NormalFont, AutoSize = true, Location = new Point(370, 16) });
            cmbFilter = new ComboBox
            {
                Location = new Point(420, 12),
                Size = new Size(140, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFilter.Items.AddRange(new object[] { "All active", "Low stock", "Out of stock" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += (s, e) => LoadData();
            UiHelper.StyleComboBox(cmbFilter);
            top.Controls.Add(cmbFilter);

            Button btnRefresh = new Button { Text = "Refresh (F5)", Location = new Point(580, 10), Size = new Size(120, 34) };
            UiHelper.StyleAccentButton(btnRefresh, FormAccent.Inventory, FormAccent.InventoryDark);
            btnRefresh.Click += (s, e) => LoadData();
            top.Controls.Add(btnRefresh);

            lblCount = new Label
            {
                Font = UiHelper.SmallFont,
                ForeColor = Color.FromArgb(110, 122, 136),
                AutoSize = true,
                Location = new Point(720, 18)
            };
            top.Controls.Add(lblCount);
            this.Controls.Add(top);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.Inventory);
            this.Controls.Add(dgv);
            dgv.BringToFront();
        }

        private void LoadData()
        {
            string kw = txtSearch != null ? txtSearch.Text.Trim() : "";
            List<Product> list = string.IsNullOrWhiteSpace(kw) ? repo.GetAllActive() : repo.Search(kw);

            string filter = cmbFilter != null && cmbFilter.SelectedItem != null
                ? cmbFilter.SelectedItem.ToString()
                : "All active";

            if (filter == "Low stock")
                list = list.Where(p => p.CurrentStock > 0 && p.CurrentStock <= p.MinimumStock).ToList();
            else if (filter == "Out of stock")
                list = list.Where(p => p.CurrentStock <= 0).ToList();

            dgv.DataSource = null;
            dgv.DataSource = list;
            foreach (var h in new[] { "ProductID", "CategoryID", "IsActive", "CreatedDate" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
            if (dgv.Columns.Contains("StockValue")) dgv.Columns["StockValue"].HeaderText = "Stock Value";
            if (dgv.Columns.Contains("UnitSalePrice")) dgv.Columns["UnitSalePrice"].HeaderText = "Unit Sale";
            if (dgv.Columns.Contains("PackSize")) dgv.Columns["PackSize"].HeaderText = "Pack Size";
            if (dgv.Columns.Contains("UnitOfMeasure")) dgv.Columns["UnitOfMeasure"].HeaderText = "UOM";

            int low = 0, outOf = 0;
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["CurrentStock"].Value == null) continue;
                int stock = Convert.ToInt32(row.Cells["CurrentStock"].Value);
                int min = row.Cells["MinimumStock"].Value != null
                    ? Convert.ToInt32(row.Cells["MinimumStock"].Value) : 0;
                if (stock <= 0)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 205, 210);
                    outOf++;
                }
                else if (stock <= min)
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
                    low++;
                }
            }

            if (lblCount != null)
                lblCount.Text = list.Count + " shown  ·  " + low + " low  ·  " + outOf + " out";
        }
    }
}
