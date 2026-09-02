using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    public partial class NewItemForm : Form
    {
        private readonly ProductRepository productRepo = new ProductRepository();
        private readonly CategoryRepository categoryRepo = new CategoryRepository();
        private readonly ProductService _productService = new ProductService();
        private TextBox txtName, txtCode, txtBarcode, txtPurchase, txtSale, txtDisc, txtMinStock, txtPackSize;
        private bool priceCalcBusy;
        private ComboBox cmbCategory, cmbUom;
        private CheckBox chkCategory, chkEditExisting, chkUom;
        private Label lblUnitPreview, lblMode;
        private int? editingProductId = null;

        public NewItemForm()
        {
            InitializeComponent();
            cmbCategory.DataSource = categoryRepo.GetAllActive();
            cmbCategory.DisplayMember = "CategoryName";
            cmbCategory.ValueMember = "CategoryID";
            string next = _productService.NextCode();
            txtCode.Text = next; txtBarcode.Text = next;
            chkCategory.Checked = false;
            cmbCategory.Enabled = false;
            chkUom.Checked = false;
            cmbUom.Enabled = false;
            txtPackSize.Enabled = true;
            txtPackSize.Text = "1";
            UpdateUnitPreview();
            txtName.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "New / Edit Item";
            this.Size = new Size(640, 700);
            this.MinimumSize = new Size(540, 600);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F12) Save(); };

            this.Controls.Add(UiHelper.CreateFormBanner(
                "NEW / EDIT ITEM",
                "TP + Disc% auto-updates RP  ·  Tick EDIT EXISTING to load by code",
                FormAccent.NewItem, FormAccent.NewItemDark));

            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 16, 28, 16) };
            this.Controls.Add(card);

            int y = 8;

            Panel modeBar = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(560, 40),
                BackColor = Color.FromArgb(243, 229, 245)
            };
            chkEditExisting = new CheckBox
            {
                Text = "EDIT EXISTING PRODUCT  (tick → type Product Code → Enter to load)",
                Font = UiHelper.HeaderFont,
                ForeColor = FormAccent.NewItemDark,
                Location = new Point(8, 8),
                Size = new Size(540, 28),
                Checked = false
            };
            chkEditExisting.CheckedChanged += (s, e) =>
            {
                if (chkEditExisting.Checked)
                {
                    txtCode.ReadOnly = false;
                    txtCode.BackColor = Color.FromArgb(255, 249, 196);
                    txtCode.Clear();
                    modeBar.BackColor = Color.FromArgb(255, 236, 179);
                    lblMode.Text = "MODE: EDIT — enter product code and press Enter";
                    lblMode.ForeColor = Color.FromArgb(183, 110, 0);
                    txtCode.Focus();
                }
                else
                {
                    ResetNewMode();
                    modeBar.BackColor = Color.FromArgb(243, 229, 245);
                }
            };
            modeBar.Controls.Add(chkEditExisting);
            card.Controls.Add(modeBar);
            y += 48;

            lblMode = new Label
            {
                Text = "MODE: NEW PRODUCT",
                Font = UiHelper.SmallFont,
                ForeColor = FormAccent.NewItem,
                Location = new Point(0, y),
                AutoSize = true
            };
            card.Controls.Add(lblMode);
            y += 28;

            AddL(card, "Product Code", 0, y);
            txtCode = AddT(card, 150, y, 140);
            txtCode.ReadOnly = true;
            txtCode.BackColor = Color.FromArgb(245, 247, 250);
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

            AddL(card, "Pack size", 0, y);
            txtPackSize = AddT(card, 150, y, 100);
            txtPackSize.Text = "1";
            txtPackSize.Enabled = true;
            txtPackSize.TextChanged += (s, e) => UpdateUnitPreview();
            card.Controls.Add(new Label
            {
                Text = "e.g. 50 for 50kg — prices below are for ONE PACK",
                Font = UiHelper.SmallFont,
                ForeColor = Color.Gray,
                Location = new Point(260, y + 4),
                AutoSize = true
            });
            y += 42;

            AddL(card, "TP Purchase (pack)", 0, y); txtPurchase = AddT(card, 150, y, 120);
            txtPurchase.KeyDown += Next;
            txtPurchase.TextChanged += (s, e) => { OnTpOrDiscChanged(); UpdateUnitPreview(); };
            y += 42;

            AddL(card, "Disc %", 0, y); txtDisc = AddT(card, 150, y, 80);
            txtDisc.Text = "0";
            txtDisc.KeyDown += Next;
            txtDisc.TextChanged += (s, e) => { OnTpOrDiscChanged(); UpdateUnitPreview(); };
            card.Controls.Add(new Label
            {
                Text = "RP = TP × (1 + Disc%/100)  ·  edit RP to reverse-calc Disc%",
                Font = UiHelper.SmallFont,
                ForeColor = Color.Gray,
                Location = new Point(250, y + 6),
                AutoSize = true
            });
            y += 42;

            AddL(card, "RP Sale (pack)", 0, y); txtSale = AddT(card, 150, y, 120);
            txtSale.KeyDown += Next;
            txtSale.TextChanged += (s, e) => { OnRpChanged(); UpdateUnitPreview(); };
            lblUnitPreview = new Label
            {
                Text = "Unit sale / unit cost: —",
                Font = UiHelper.HeaderFont,
                ForeColor = FormAccent.NewItem,
                Location = new Point(280, y + 2),
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

        private void OnTpOrDiscChanged()
        {
            if (priceCalcBusy || txtPurchase == null || txtDisc == null || txtSale == null) return;
            priceCalcBusy = true;
            decimal tp = 0, disc = 0;
            decimal.TryParse(txtPurchase.Text, out tp);
            decimal.TryParse(txtDisc.Text, out disc);
            if (disc < 0) disc = 0;
            decimal rp = Math.Round(tp * (1m + disc / 100m), 2);
            txtSale.Text = rp.ToString("0.00");
            priceCalcBusy = false;
        }

        private void OnRpChanged()
        {
            if (priceCalcBusy || txtPurchase == null || txtDisc == null || txtSale == null) return;
            priceCalcBusy = true;
            decimal tp = 0, rp = 0;
            decimal.TryParse(txtPurchase.Text, out tp);
            decimal.TryParse(txtSale.Text, out rp);
            decimal disc = 0;
            if (tp > 0) disc = Math.Round((rp - tp) * 100m / tp, 2);
            if (disc < 0) disc = 0;
            txtDisc.Text = disc.ToString("0.##");
            priceCalcBusy = false;
        }

        private void UpdateUnitPreview()
        {
            if (lblUnitPreview == null) return;
            decimal sale = 0, pur = 0, pack = 1;
            if (txtSale != null) decimal.TryParse(txtSale.Text, out sale);
            if (txtPurchase != null) decimal.TryParse(txtPurchase.Text, out pur);
            if (txtPackSize != null) decimal.TryParse(txtPackSize.Text, out pack);
            if (pack <= 0) pack = 1;
            decimal unitSale = pack == 1m ? sale : Math.Round(sale / pack, 4);
            decimal unitCost = pack == 1m ? pur : Math.Round(pur / pack, 4);
            lblUnitPreview.Text = "Unit sale: " + unitSale.ToString("0.####") +
                "   |   Unit cost: " + unitCost.ToString("0.####");
        }

        private void LoadByCode()
        {
            string code = txtCode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;
            var p = productRepo.GetByCode(code);
            if (p == null) { DialogHelpers.Error(this, "Product code not found."); editingProductId = null; return; }
            editingProductId = p.ProductID;
            txtName.Text = p.ProductName;
            txtBarcode.Text = p.Barcode ?? p.ProductCode;
            priceCalcBusy = true;
            txtPurchase.Text = p.PurchasePrice.ToString("0.00");
            txtSale.Text = p.SalePrice.ToString("0.00");
            decimal d0 = 0;
            if (p.PurchasePrice > 0)
                d0 = Math.Round((p.SalePrice - p.PurchasePrice) * 100m / p.PurchasePrice, 2);
            if (d0 < 0) d0 = 0;
            if (txtDisc != null) txtDisc.Text = d0.ToString("0.##");
            priceCalcBusy = false;
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
            if (p.CategoryID > 0 && cmbCategory.DataSource != null)
            {
                chkCategory.Checked = true;
                try { cmbCategory.SelectedValue = p.CategoryID; } catch { }
            }
            lblMode.Text = "MODE: EDIT — " + p.ProductCode + "  " + p.ProductName;
            lblMode.ForeColor = Color.FromArgb(183, 110, 0);
            UpdateUnitPreview();
            txtName.Focus();
        }

        private void ResetNewMode()
        {
            editingProductId = null;
            txtCode.ReadOnly = true;
            txtCode.BackColor = Color.FromArgb(245, 247, 250);
            string next = _productService.NextCode();
            txtCode.Text = next; txtBarcode.Text = next;
            txtName.Clear(); txtPurchase.Clear(); txtSale.Clear(); if (txtDisc != null) txtDisc.Text = "0"; txtMinStock.Text = "0";
            chkCategory.Checked = false; cmbCategory.Enabled = false;
            chkUom.Checked = false; cmbUom.Enabled = false; cmbUom.SelectedIndex = -1;
            txtPackSize.Enabled = true; txtPackSize.Text = "1";
            if (lblMode != null)
            {
                lblMode.Text = "MODE: NEW PRODUCT";
                lblMode.ForeColor = FormAccent.NewItem;
            }
            UpdateUnitPreview();
        }

        private void Next(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectNextControl((Control)s, true, true, true, true); } }
        private void AddL(Control parent, string t, int x, int y) { parent.Controls.Add(new Label { Text = t, Font = UiHelper.NormalFont, Location = new Point(x, y + 4), Size = new Size(150, 22) }); }
        private TextBox AddT(Control parent, int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 28) }; UiHelper.StyleTextBox(t); parent.Controls.Add(t); return t; }

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { DialogHelpers.Error(this, "Name required."); return; }
                decimal pur = 0, sale = 0, pack = 1; int min = 0;
                decimal.TryParse(txtPurchase.Text, out pur);
                decimal.TryParse(txtSale.Text, out sale);
                int.TryParse(txtMinStock.Text, out min);
                if (!decimal.TryParse(txtPackSize.Text, out pack) || pack <= 0)
                {
                    DialogHelpers.Error(this, "Pack size must be a number greater than 0.");
                    txtPackSize.Focus();
                    return;
                }
                string uom = chkUom.Checked && cmbUom.SelectedItem != null ? cmbUom.SelectedItem.ToString() : null;

                if (chkEditExisting.Checked && editingProductId.HasValue)
                {
                    if (!DialogHelpers.Confirm(this, "Update this product?\nTP: " + pur.ToString("0.00") +
                            "\nRP: " + sale.ToString("0.00") +
                            "\nPack size: " + pack.ToString("0.####"))) return;
                    _productService.Update(editingProductId.Value, txtName.Text.Trim(), pur, sale, min, true, uom, pack);
                    DialogHelpers.Info(this, "Product updated.\nUnit cost: " + Math.Round(pur / pack, 4).ToString("0.####") +
                        "\nUnit sale: " + Math.Round(sale / pack, 4).ToString("0.####"));
                    this.Tag = "NOSAVECONFIRM";
                    chkEditExisting.Checked = false;
                    return;
                }

                int catId = 1;
                if (chkCategory.Checked && cmbCategory.SelectedValue != null) catId = (int)cmbCategory.SelectedValue;
                else if (cmbCategory.Items.Count > 0) catId = ((Category)cmbCategory.Items[0]).CategoryID;

                var product = new Product
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
                };

                if (!DialogHelpers.Confirm(this, "Save new product?")) return;
                int newId = _productService.Create(product);
                try { _productService.Update(newId, product.ProductName, pur, sale, min, true, uom, pack); }
                catch { }

                DialogHelpers.Info(this, "Saved!\nUnit cost: " + Math.Round(pur / pack, 4).ToString("0.####") +
                    "\nUnit sale: " + Math.Round(sale / pack, 4).ToString("0.####"));
                this.Tag = "NOSAVECONFIRM";
                ResetNewMode();
                txtName.Focus();
            }
            catch (Exception ex) { DialogHelpers.Error(this, ex.Message); }
        }
    }
}
