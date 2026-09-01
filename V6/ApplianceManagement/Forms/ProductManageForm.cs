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
        private TextBox txtSearch, txtName, txtSale, txtPur, txtMin, txtPack;
        private CheckBox chkActive;
        private Label lblStatus;
        private Product selected;

        public ProductManageForm()
        {
            InitializeComponent();
            LoadGrid("");
        }

        private void InitializeComponent()
        {
            this.Text = "Manage Products";
            this.Size = new Size(980, 600);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            UiHelper.AttachEnterNavigation(this);

            Controls.Add(UiHelper.CreateFormBanner(
                "PRODUCTS",
                "List · Edit rates/pack · Disable / Reactivate  ·  Prices are PACK prices",
                FormAccent.NewItem, FormAccent.NewItemDark));

            var top = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.White, Padding = new Padding(12, 10, 12, 8) };
            top.Controls.Add(new Label { Text = "Search:", Font = UiHelper.NormalFont, Location = new Point(8, 12), AutoSize = true });
            txtSearch = new TextBox { Location = new Point(70, 8), Size = new Size(320, 26) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => LoadGrid(txtSearch.Text);
            top.Controls.Add(txtSearch);
            Controls.Add(top);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.NewItem);
            dgv.ReadOnly = true;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.SelectionChanged += (s, e) =>
            {
                if (dgv.CurrentRow == null || dgv.CurrentRow.DataBoundItem == null) return;
                selected = dgv.CurrentRow.DataBoundItem as Product;
                if (selected == null) return;
                txtName.Text = selected.ProductName;
                txtSale.Text = selected.SalePrice.ToString("0.00");
                txtPur.Text = selected.PurchasePrice.ToString("0.00");
                txtMin.Text = selected.MinimumStock.ToString();
                txtPack.Text = selected.PackSize > 0 ? selected.PackSize.ToString("0.####") : "1";
                chkActive.Checked = selected.IsActive;
                lblStatus.Text = selected.IsActive ? "Status: ACTIVE" : "Status: DISABLED";
                lblStatus.ForeColor = selected.IsActive ? Color.FromArgb(46, 125, 50) : Color.FromArgb(198, 40, 40);
            };
            Controls.Add(dgv);

            Panel edit = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Color.White, Padding = new Padding(16) };
            int y = 8;
            edit.Controls.Add(new Label { Text = "Edit Product", Font = UiHelper.HeaderFont, Location = new Point(0, y), AutoSize = true });
            y += 36;
            edit.Controls.Add(new Label { Text = "Name", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 18;
            txtName = new TextBox { Location = new Point(0, y), Size = new Size(260, 26) }; UiHelper.StyleTextBox(txtName); edit.Controls.Add(txtName); y += 36;
            edit.Controls.Add(new Label { Text = "Sale Price (pack)", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 18;
            txtSale = new TextBox { Location = new Point(0, y), Size = new Size(140, 26) }; UiHelper.StyleTextBox(txtSale); edit.Controls.Add(txtSale); y += 36;
            edit.Controls.Add(new Label { Text = "Purchase Price (pack)", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 18;
            txtPur = new TextBox { Location = new Point(0, y), Size = new Size(140, 26) }; UiHelper.StyleTextBox(txtPur); edit.Controls.Add(txtPur); y += 36;
            edit.Controls.Add(new Label { Text = "Pack size", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 18;
            txtPack = new TextBox { Location = new Point(0, y), Size = new Size(100, 26), Text = "1" }; UiHelper.StyleTextBox(txtPack); edit.Controls.Add(txtPack); y += 36;
            edit.Controls.Add(new Label { Text = "Min Stock", Location = new Point(0, y), AutoSize = true, Font = UiHelper.SmallFont });
            y += 18;
            txtMin = new TextBox { Location = new Point(0, y), Size = new Size(80, 26) }; UiHelper.StyleTextBox(txtMin); edit.Controls.Add(txtMin); y += 36;
            chkActive = new CheckBox { Text = "Active (sale/purchase)", Font = UiHelper.NormalFont, Location = new Point(0, y), Size = new Size(260, 24), Checked = true };
            edit.Controls.Add(chkActive); y += 32;
            lblStatus = new Label { Text = "Status: —", Font = UiHelper.SmallFont, Location = new Point(0, y), AutoSize = true };
            edit.Controls.Add(lblStatus); y += 28;

            Button btnSave = new Button { Text = "SAVE CHANGES", Location = new Point(0, y), Size = new Size(160, 34) };
            UiHelper.StyleAccentButton(btnSave, FormAccent.NewItem, FormAccent.NewItemDark);
            btnSave.Click += BtnSave_Click;
            edit.Controls.Add(btnSave); y += 42;

            Button btnDisable = new Button { Text = "DISABLE", Location = new Point(0, y), Size = new Size(120, 32) };
            UiHelper.StyleAccentButton(btnDisable, FormAccent.LowStock, FormAccent.LowStockDark);
            btnDisable.Click += (s, e) =>
            {
                if (selected == null) return;
                if (!DialogHelpers.Confirm(this, "Disable this product? It will not show in Sale/Purchase.")) return;
                repo.SetActive(selected.ProductID, false);
                DialogHelpers.Info(this, "Disabled.");
                LoadGrid(txtSearch.Text);
            };
            edit.Controls.Add(btnDisable);

            Button btnReactivate = new Button { Text = "REACTIVATE", Location = new Point(130, y), Size = new Size(120, 32) };
            UiHelper.StyleAccentButton(btnReactivate, FormAccent.Purchase, FormAccent.PurchaseDark);
            btnReactivate.Click += (s, e) =>
            {
                if (selected == null) return;
                if (!DialogHelpers.Confirm(this, "Reactivate this product for sale/purchase?")) return;
                repo.SetActive(selected.ProductID, true);
                DialogHelpers.Info(this, "Reactivated.");
                LoadGrid(txtSearch.Text);
            };
            edit.Controls.Add(btnReactivate);
            Controls.Add(edit);
            edit.BringToFront();
        }

        private void LoadGrid(string kw)
        {
            var list = string.IsNullOrWhiteSpace(kw) ? repo.GetAllForManage() : repo.SearchAll(kw);
            dgv.DataSource = null;
            dgv.DataSource = list;
            foreach (var h in new[] { "ProductID", "CategoryID", "CreatedDate", "UnitOfMeasure", "Barcode" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (selected == null) { DialogHelpers.Error(this, "Select a product."); return; }
            decimal sale = 0, pur = 0, pack = 1; int min = 0;
            decimal.TryParse(txtSale.Text, out sale);
            decimal.TryParse(txtPur.Text, out pur);
            int.TryParse(txtMin.Text, out min);
            if (!decimal.TryParse(txtPack.Text, out pack) || pack <= 0)
            {
                DialogHelpers.Error(this, "Pack size must be greater than 0.");
                return;
            }
            if (pur < 0 || sale < 0)
            {
                DialogHelpers.Error(this, "Prices cannot be negative.");
                return;
            }
            if (!DialogHelpers.Confirm(this, "Save changes to " + selected.ProductCode + "?")) return;
            repo.UpdateFull(selected.ProductID, txtName.Text.Trim(), pur, sale, min, chkActive.Checked, selected.UnitOfMeasure, pack);
            DialogHelpers.Info(this, "Updated.\nUnit cost: " + Math.Round(pur / pack, 4).ToString("0.####") +
                "\nUnit sale: " + Math.Round(sale / pack, 4).ToString("0.####"));
            LoadGrid(txtSearch.Text);
        }
    }
}
