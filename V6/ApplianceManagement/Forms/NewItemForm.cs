using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class NewItemForm : Form
    {
        private ProductRepository productRepo = new ProductRepository();
        private CategoryRepository categoryRepo = new CategoryRepository();
        private TextBox txtName, txtCode, txtBarcode, txtPurchase, txtSale, txtMinStock, txtPackSize;
        private ComboBox cmbCategory, cmbUom;
        private CheckBox chkCategory, chkEditExisting, chkUom;
        private int? editingProductId = null;

        public NewItemForm()
        {
            InitializeComponent();
            cmbCategory.DataSource = categoryRepo.GetAllActive();
            cmbCategory.DisplayMember = "CategoryName";
            cmbCategory.ValueMember = "CategoryID";
            string next = productRepo.GetNextProductCode();
            txtCode.Text = next; txtBarcode.Text = next;
            // Default: category + UOM unchecked/disabled
            chkCategory.Checked = false;
            cmbCategory.Enabled = false;
            chkUom.Checked = false;
            cmbUom.Enabled = false;
            txtPackSize.Enabled = false;
            txtName.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "New Item";
            this.Size = new Size(600, 580);
            this.MinimumSize = new Size(520, 500);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F12) Save(); };

            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 20, 28, 20) };
            this.Controls.Add(card);

            int y = 12;
            AddL(card, "Product Code", 0, y);
            txtCode = AddT(card, 150, y, 140);
            txtCode.ReadOnly = true;
            txtCode.BackColor = Color.FromArgb(245, 247, 250);
            chkEditExisting = new CheckBox
            {
                Text = "Edit existing (enter code)",
                Font = UiHelper.SmallFont,
                Location = new Point(300, y + 2),
                Size = new Size(220, 24)
            };
            chkEditExisting.CheckedChanged += (s, e) =>
            {
                if (chkEditExisting.Checked)
                {
                    txtCode.ReadOnly = false;
                    txtCode.BackColor = Color.White;
                    txtCode.Clear();
                    txtCode.Focus();
                }
                else ResetNewMode();
            };
            card.Controls.Add(chkEditExisting);
            // Enter on code loads product when edit mode
            txtCode.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (chkEditExisting.Checked) LoadByCode();
                    else SelectNextControl(txtCode, true, true, true, true);
                }
            };
            y += 42;

            AddL(card, "Product Name", 0, y); txtName = AddT(card, 150, y, 300); txtName.KeyDown += Next; y += 42;
            AddL(card, "Barcode", 0, y); txtBarcode = AddT(card, 150, y, 220); txtBarcode.KeyDown += Next; y += 42;

            chkCategory = new CheckBox { Text = "Category", Font = UiHelper.NormalFont, Location = new Point(0, y), Size = new Size(140, 26), Checked = false };
            chkCategory.CheckedChanged += (s, e) => cmbCategory.Enabled = chkCategory.Checked;
            // Right-click enables category
            chkCategory.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) { chkCategory.Checked = true; cmbCategory.Enabled = true; } };
            card.Controls.Add(chkCategory);
            cmbCategory = new ComboBox { Location = new Point(150, y), Size = new Size(300, 26), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            UiHelper.StyleComboBox(cmbCategory); card.Controls.Add(cmbCategory); y += 42;

            chkUom = new CheckBox { Text = "Unit of measure", Font = UiHelper.NormalFont, Location = new Point(0, y), Size = new Size(140, 26), Checked = false };
            chkUom.CheckedChanged += (s, e) =>
            {
                cmbUom.Enabled = chkUom.Checked;
                txtPackSize.Enabled = chkUom.Checked;
                if (!chkUom.Checked) { cmbUom.SelectedIndex = -1; txtPackSize.Text = "1"; }
            };
            chkUom.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) { chkUom.Checked = true; cmbUom.Enabled = true; txtPackSize.Enabled = true; } };
            card.Controls.Add(chkUom);
            cmbUom = new ComboBox { Location = new Point(150, y), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            UiHelper.StyleComboBox(cmbUom);
            cmbUom.Items.AddRange(new object[] { "Piece", "Kilograms", "Grams", "Litre", "Meter" });
            card.Controls.Add(cmbUom);
            card.Controls.Add(new Label { Text = "Pack size", Font = UiHelper.SmallFont, Location = new Point(320, y + 4), AutoSize = true });
            txtPackSize = AddT(card, 390, y, 80); txtPackSize.Text = "1"; txtPackSize.Enabled = false; y += 46;

            AddL(card, "Purchase Price", 0, y); txtPurchase = AddT(card, 150, y, 160); txtPurchase.KeyDown += Next; y += 42;
            AddL(card, "Sale Price", 0, y); txtSale = AddT(card, 150, y, 160); txtSale.KeyDown += Next;
            card.Controls.Add(new Label { Text = "(pack price; unit = price / pack size)", Font = UiHelper.SmallFont, ForeColor = Color.Gray, Location = new Point(320, y + 4), AutoSize = true });
            y += 42;
            AddL(card, "Min Stock", 0, y); txtMinStock = AddT(card, 150, y, 100); txtMinStock.Text = "0"; txtMinStock.KeyDown += Next; y += 56;

            Button btnSave = new Button { Text = "SAVE (F12)", Location = new Point(150, y), Size = new Size(140, 38) };
            UiHelper.StyleButton(btnSave); btnSave.Click += (s, e) => Save();
            Button btnClose = new Button { Text = "CLOSE (F4)", Location = new Point(304, y), Size = new Size(140, 38) };
            UiHelper.StyleButton(btnClose); btnClose.Click += (s, e) => this.Close();
            card.Controls.Add(btnSave); card.Controls.Add(btnClose);
        }

        private void LoadByCode()
        {
            string code = txtCode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;
            var p = productRepo.GetByCode(code);
            if (p == null) { MessageBox.Show("Product code not found."); editingProductId = null; return; }
            editingProductId = p.ProductID;
            txtName.Text = p.ProductName;
            txtBarcode.Text = p.Barcode ?? p.ProductCode;
            txtPurchase.Text = p.PurchasePrice.ToString("0.00");
            txtSale.Text = p.SalePrice.ToString("0.00");
            txtMinStock.Text = p.MinimumStock.ToString();
            if (!string.IsNullOrEmpty(p.UnitOfMeasure))
            {
                chkUom.Checked = true;
                if (cmbUom.Items.Contains(p.UnitOfMeasure)) cmbUom.SelectedItem = p.UnitOfMeasure;
                txtPackSize.Text = p.PackSize > 0 ? p.PackSize.ToString("0.####") : "1";
            }
            txtName.Focus();
        }

        private void ResetNewMode()
        {
            editingProductId = null;
            txtCode.ReadOnly = true;
            txtCode.BackColor = Color.FromArgb(245, 247, 250);
            string next = productRepo.GetNextProductCode();
            txtCode.Text = next; txtBarcode.Text = next;
            txtName.Clear(); txtPurchase.Clear(); txtSale.Clear(); txtMinStock.Text = "0";
            chkCategory.Checked = false; cmbCategory.Enabled = false;
            chkUom.Checked = false; cmbUom.Enabled = false; txtPackSize.Enabled = false; txtPackSize.Text = "1";
        }

        private void Next(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectNextControl((Control)s, true, true, true, true); } }
        private void AddL(Control parent, string t, int x, int y) { parent.Controls.Add(new Label { Text = t, Font = UiHelper.NormalFont, Location = new Point(x, y + 4), Size = new Size(140, 22) }); }
        private TextBox AddT(Control parent, int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 28) }; UiHelper.StyleTextBox(t); parent.Controls.Add(t); return t; }

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Name required."); return; }
                decimal pur = 0, sale = 0, pack = 1; int min = 0;
                decimal.TryParse(txtPurchase.Text, out pur); decimal.TryParse(txtSale.Text, out sale); int.TryParse(txtMinStock.Text, out min);
                decimal.TryParse(txtPackSize.Text, out pack); if (pack <= 0) pack = 1;
                string uom = chkUom.Checked && cmbUom.SelectedItem != null ? cmbUom.SelectedItem.ToString() : null;

                if (chkEditExisting.Checked && editingProductId.HasValue)
                {
                    productRepo.UpdateFull(editingProductId.Value, txtName.Text.Trim(), pur, sale, min, true, uom, pack);
                    MessageBox.Show("Product updated.");
                    chkEditExisting.Checked = false;
                    return;
                }

                if (productRepo.ExistsCode(txtCode.Text.Trim())) { MessageBox.Show("Code exists."); return; }
                int catId = 1;
                if (chkCategory.Checked && cmbCategory.SelectedValue != null) catId = (int)cmbCategory.SelectedValue;
                else if (cmbCategory.Items.Count > 0) catId = ((Category)cmbCategory.Items[0]).CategoryID;
                productRepo.Insert(new Product
                {
                    ProductCode = txtCode.Text.Trim(),
                    Barcode = string.IsNullOrWhiteSpace(txtBarcode.Text) ? txtCode.Text.Trim() : txtBarcode.Text.Trim(),
                    ProductName = txtName.Text.Trim(), CategoryID = catId,
                    PurchasePrice = pur, SalePrice = sale, MinimumStock = min,
                    UnitOfMeasure = uom, PackSize = pack
                });
                MessageBox.Show("Saved!");
                ResetNewMode();
                txtName.Focus();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
