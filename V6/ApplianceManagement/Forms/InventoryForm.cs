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

        public InventoryForm()
        {
            InitializeComponent();
            LoadData("");
            txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "Stock Position";
            this.Size = new Size(960, 560);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);

            this.Controls.Add(new Label { Text = "Search:", Font = UiHelper.NormalFont, Location = new Point(15, 15), Size = new Size(55, 22) });
            txtSearch = new TextBox { Location = new Point(75, 12), Size = new Size(300, 26) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadData(txtSearch.Text);
            this.Controls.Add(txtSearch);

            dgv = new DataGridView { Location = new Point(15, 50), Size = new Size(920, 460) };
            UiHelper.StyleGrid(dgv);
            this.Controls.Add(dgv);
        }

        private void LoadData(string kw)
        {
            var list = string.IsNullOrWhiteSpace(kw) ? repo.GetAllActive() : repo.Search(kw);
            dgv.DataSource = null;
            dgv.DataSource = list;
            foreach (var h in new[] { "ProductID", "CategoryID", "IsActive", "CreatedDate" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
            if (dgv.Columns.Contains("StockValue")) dgv.Columns["StockValue"].HeaderText = "Stock Value";
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.Cells["CurrentStock"].Value != null && row.Cells["MinimumStock"].Value != null)
                {
                    int stock = Convert.ToInt32(row.Cells["CurrentStock"].Value);
                    int min = Convert.ToInt32(row.Cells["MinimumStock"].Value);
                    if (stock <= min) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 220, 220);
                }
            }
        }
    }
}
