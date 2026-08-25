using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class ProductManageForm : Form
    {
        private ProductRepository repo = new ProductRepository();
        private DataGridView dgv;
        private TextBox txtSearch, txtName, txtSale, txtPur, txtMin;
        private CheckBox chkActive;
        private Product selected;

        public ProductManageForm()
        {
            InitializeComponent();
            LoadGrid("");
        }

        private void InitializeComponent()
        {
            this.Text = "Manage Products";
            this.Size = new Size(960, 560);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);

            this.Controls.Add(new Label { Text = "Search:", Font = UiHelper.NormalFont, Location = new Point(15, 15), Size = new Size(55, 22) });
            txtSearch = new TextBox { Location = new Point(75, 12), Size = new Size(280, 26) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid(txtSearch.Text);
            this.Controls.Add(txtSearch);

            dgv = new DataGridView { Location = new Point(15, 50), Size = new Size(600, 450) };
            UiHelper.StyleGrid(dgv);
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.CurrentRow == null || dgv.CurrentRow.DataBoundItem == null) return;
                selected = dgv.CurrentRow.DataBoundItem as Product;
                if (selected == null) return;
                txtName.Text = selected.ProductName;
                txtSale.Text = selected.SalePrice.ToString("0.00");
                txtPur.Text = selected.PurchasePrice.ToString("0.00");
                txtMin.Text = selected.MinimumStock.ToString();
                chkActive.Checked = selected.IsActive;
            };
            this.Controls.Add(dgv);

            Panel edit = new Panel { Location = new Point(635, 50), Size = new Size(300, 400), BackColor = Color.White };
            int y = 15;
            edit.Controls.Add(new Label { Text = "Edit Product", Font = UiHelper.HeaderFont, Location = new Point(15, y), Size = new Size(200, 24) }); y += 40;
            edit.Controls.Add(new Label { Text = "Name:", Location = new Point(15, y), Size = new Size(80, 22), Font = UiHelper.NormalFont });
            txtName = new TextBox { Location = new Point(100, y - 2), Size = new Size(180, 26) }; UiHelper.StyleTextBox(txtName); edit.Controls.Add(txtName); y += 36;
            edit.Controls.Add(new Label { Text = "Sale Price:", Location = new Point(15, y), Size = new Size(80, 22), Font = UiHelper.NormalFont });
            txtSale = new TextBox { Location = new Point(100, y - 2), Size = new Size(120, 26) }; UiHelper.StyleTextBox(txtSale); edit.Controls.Add(txtSale); y += 36;
            edit.Controls.Add(new Label { Text = "Purchase:", Location = new Point(15, y), Size = new Size(80, 22), Font = UiHelper.NormalFont });
            txtPur = new TextBox { Location = new Point(100, y - 2), Size = new Size(120, 26) }; UiHelper.StyleTextBox(txtPur); edit.Controls.Add(txtPur); y += 36;
            edit.Controls.Add(new Label { Text = "Min Stock:", Location = new Point(15, y), Size = new Size(80, 22), Font = UiHelper.NormalFont });
            txtMin = new TextBox { Location = new Point(100, y - 2), Size = new Size(80, 26) }; UiHelper.StyleTextBox(txtMin); edit.Controls.Add(txtMin); y += 36;
            chkActive = new CheckBox { Text = "Active (for sale/purchase)", Font = UiHelper.NormalFont, Location = new Point(15, y), Size = new Size(250, 24), Checked = true };
            edit.Controls.Add(chkActive); y += 40;

            Button btnSave = new Button { Text = "Save Changes", Location = new Point(15, y), Size = new Size(140, 34) };
            UiHelper.StyleButton(btnSave);
            btnSave.Click += BtnSave_Click;
            edit.Controls.Add(btnSave); y += 45;

            Button btnDisable = new Button { Text = "Disable (Not for sale)", Location = new Point(15, y), Size = new Size(180, 34) };
            UiHelper.StyleButton(btnDisable);
            btnDisable.Click += (s, e) =>
            {
                if (selected == null) return;
                if (MessageBox.Show("Disable this product? It will not show in Sale/Purchase.", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                repo.SetActive(selected.ProductID, false);
                MessageBox.Show("Disabled.");
                LoadGrid(txtSearch.Text);
            };
            edit.Controls.Add(btnDisable);
            this.Controls.Add(edit);
        }

        private void LoadGrid(string kw)
        {
            // Show all including inactive for management
            var list = string.IsNullOrWhiteSpace(kw) ? repo.GetAllForManage() : repo.SearchAll(kw);
            dgv.DataSource = null;
            dgv.DataSource = list;
            foreach (var h in new[] { "ProductID", "CategoryID", "CreatedDate" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (selected == null) { MessageBox.Show("Select a product."); return; }
            decimal sale = 0, pur = 0; int min = 0;
            decimal.TryParse(txtSale.Text, out sale);
            decimal.TryParse(txtPur.Text, out pur);
            int.TryParse(txtMin.Text, out min);
            repo.Update(selected.ProductID, txtName.Text.Trim(), pur, sale, min, chkActive.Checked);
            MessageBox.Show("Updated.");
            LoadGrid(txtSearch.Text);
        }
    }
}
