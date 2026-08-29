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
        private Label lblUnitPreview;
        private int? editingProductId = null;

        public NewItemForm()
        {
            InitializeComponent();
            cmbCategory.DataSource = categoryRepo.GetAllActive();
            cmbCategory.DisplayMember = "CategoryName";
            cmbCategory.ValueMember = "CategoryID";
            string next = productRepo.GetNextProductCode();
            txtCode.Text = next; txtBarcode.Text = next;
            chkCategory.Checked = false;
            cmbCategory.Enabled = false;
            chkUom.Checked = false;
            cmbUom.Enabled = false;
            // Pack size ALWAYS enabled so price division works without UOM
            txtPackSize.Enabled = true;
            txtPackSize.Text = "1";
            UpdateUnitPreview();
            txtName.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "New Item";
            this.Size = new Size(620, 620);
            this.MinimumSize = new Size(520, 520);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F12) Save(); };

            this.Controls.Add(UiHelper.CreateFormBanner(
                "NEW ITEM",
                "Create / edit product  ·  Pack size divides sale price into unit price",
                FormAccent.NewItem, FormAccent.NewItemDark));

            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 16, 28, 16) };
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
            chkCategory.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) { chkCategory.Checked = true; cmbCategory.Enabled = true; } };
            card.Controls.Add(chkCategory);
            cmbCategory = new ComboBox { Location = new Point(150, y), Size = new Size(300, 26), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            UiHelper.StyleComboBox(cmbCategory); card.Controls.Add(cmbCategory); y += 42;

            chkUom = new CheckBox { Text = "Unit of measure", Font = UiHelper.NormalFont, Location = new Point(0, y), Size = new Size(140, 26), Checked = false };
            chkUom.CheckedChanged += (s, e) =>
            {
                cmbUom.Enabled = chkUom.Checked;
                if (!chkUom.Checked) cmbUom.SelectedIndex = -1;
            };
            chkUom.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) { chkUom.Checked = true; cmbUom.Enabled = true; } };
            card.Controls.Add(chkUom);
            cmbUom = new ComboBox { Location = new Point(150, y), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };
            UiHelper.StyleComboBox(cmbUom);
            cmbUom.Items.AddRange(new object[] { "Piece", "Kilograms", "Grams", "Litre", "Meter" });
            card.Controls.Add(cmbUom);
            y += 42;

            // Pack size ALWAYS on (not tied to UOM)
            AddL(card, "Pack size", 0, y);
            txtPackSize = AddT(card, 150, y, 100);
            txtPackSize.Text = "1";
            txtPackSize.Enabled = true;
            txtPackSize.TextChanged += (s, e) => UpdateUnitPreview();
            card.Controls.Add(new Label
            {
                Text = "e.g. 50 for 50kg bag — Sale uses price ÷ pack",
                Font = UiHelper.SmallFont,
                ForeColor = Color.Gray,
                Location = new Point(260, y + 4),
                AutoSize = true
            });
            y += 42;

            AddL(card, "Purchase Price", 0, y); txtPurchase = AddT(card, 150, y, 160); txtPurchase.KeyDown += Next; y += 42;
            AddL(card, "Sale Price (pack)", 0, y); txtSale = AddT(card, 150, y, 160);
            txtSale.KeyDown += Next;
            txtSale.TextChanged += (s, e) => UpdateUnitPreview();
            lblUnitPreview = new Label
            {
                Text = "Unit sale price: —",
                Font = UiHelper.HeaderFont,
                ForeColor = FormAccent.NewItem,
                Location = new Point(320, y + 2),
                AutoSize = true
            };
            card.Controls.Add(lblUnitPreview);
            y += 42;
            AddL(card, "Min Stock", 0, y); txtMinStock = AddT(card, 150, y, 100); txtMinStock.Text = "0"; txtMinStock.KeyDown += Next; y += 56;

            Button btnSave = new Button { Text = "SAVE (F12)", Location = new Point(150, y), Size = new Size(140, 38) };
            UiHelper.StyleAccentButton(btnSave, FormAccent.NewItem, FormAccent.NewItemDark);
            btnSave.Click += (s, e) => Save();
            Button btnClose = new Button { Text = "CLOSE (F4)", Location = new Point(304, y), Size = new Size(140, 38) };
            UiHelper.StyleAccentButton(btnClose, FormAccent.NewItemDark, FormAccent.NewItem);
            btnClose.Click += (s, e) => this.Close();
            card.Controls.Add(btnSave); card.Controls.Add(btnClose);
        }

        private void UpdateUnitPreview()
        {
            if (lblUnitPreview == null || txtSale == null || txtPackSize == null) return;
            decimal sale = 0, pack = 1;
            decimal.TryParse(txtSale.Text, out sale);
            decimal.TryParse(txtPackSize.Text, out pack);
            if (pack <= 0) pack = 1;
            decimal unit = pack == 1m ? sale : Math.Round(sale / pack, 4);
            lblUnitPreview.Text = "Unit sale price: " + unit.ToString("0.####") +
                (pack != 1m ? ("  (" + sale.ToString("0.##") + " ÷ " + pack.ToString("0.####") + ")") : "");
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
            txtPackSize.Text = p.PackSize > 0 ? p.PackSize.ToString("0.####") : "1";
            if (!string.IsNullOrEmpty(p.UnitOfMeasure))
            {
                chkUom.Checked = true;
                if (cmbUom.Items.Contains(p.UnitOfMeasure)) cmbUom.SelectedItem = p.UnitOfMeasure;
            }
            else
            {
                chkUom.Checked = false;
                cmbUom.SelectedIndex = -1;
            }
            UpdateUnitPreview();
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
            chkUom.Checked = false; cmbUom.Enabled = false; cmbUom.SelectedIndex = -1;
            txtPackSize.Enabled = true; txtPackSize.Text = "1";
            UpdateUnitPreview();
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
                decimal.TryParse(txtPurchase.Text, out pur);
                decimal.TryParse(txtSale.Text, out sale);
                int.TryParse(txtMinStock.Text, out min);
                if (!decimal.TryParse(txtPackSize.Text, out pack) || pack <= 0)
                {
                    MessageBox.Show("Pack size must be a number greater than 0.");
                    txtPackSize.Focus();
                    return;
                }
                string uom = chkUom.Checked && cmbUom.SelectedItem != null ? cmbUom.SelectedItem.ToString() : null;

                if (chkEditExisting.Checked && editingProductId.HasValue)
                {
                    productRepo.UpdateFull(editingProductId.Value, txtName.Text.Trim(), pur, sale, min, true, uom, pack);
                    MessageBox.Show("Product updated.\nUnit sale price: " + Math.Round(sale / pack, 4).ToString("0.####"));
                    chkEditExisting.Checked = false;
                    return;
                }

                if (productRepo.ExistsCode(txtCode.Text.Trim())) { MessageBox.Show("Code exists."); return; }
                int catId = 1;
                if (chkCategory.Checked && cmbCategory.SelectedValue != null) catId = (int)cmbCategory.SelectedValue;
                else if (cmbCategory.Items.Count > 0) catId = ((Category)cmbCategory.Items[0]).CategoryID;

                int newId = productRepo.Insert(new Product
                {
                    ProductCode = txtCode.Text.Trim(),
                    Barcode = string.IsNullOrWhiteSpace(txtBarcode.Text) ? txtCode.Text.Trim() : txtBarcode.Text.Trim(),
                    ProductName = txtName.Text.Trim(),
                    CategoryID = catId,
                    PurchasePrice = pur,
                    SalePrice = sale,
                    MinimumStock = min,
                    UnitOfMeasure = uom,
                    PackSize = pack
                });

                // Ensure PackSize written even if first INSERT path skipped columns
                try { productRepo.UpdateFull(newId, txtName.Text.Trim(), pur, sale, min, true, uom, pack); }
                catch { }

                MessageBox.Show("Saved!\nUnit sale price: " + Math.Round(sale / pack, 4).ToString("0.####"));
                ResetNewMode();
                txtName.Focus();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
