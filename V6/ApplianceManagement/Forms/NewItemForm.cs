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
        private TextBox txtName, txtCode, txtBarcode, txtPurchase, txtSale, txtMinStock;
        private ComboBox cmbCategory;
        private CheckBox chkCategory, chkEditExisting;
        private int? editingProductId = null;

        public NewItemForm()
        {
            InitializeComponent();
            cmbCategory.DataSource = categoryRepo.GetAllActive();
            cmbCategory.DisplayMember = "CategoryName";
            cmbCategory.ValueMember = "CategoryID";
            string next = productRepo.GetNextProductCode();
            txtCode.Text = next; txtBarcode.Text = next;
            txtName.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "New Item";
            this.Size = new Size(580, 540);
            this.MinimumSize = new Size(520, 480);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F12) Save(); };

            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 20, 28, 20) };
            this.Controls.Add(card);

            int y = 12;
            // Product Code ABOVE name, with Edit existing checkbox
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
                else
                {
                    editingProductId = null;
                    txtCode.ReadOnly = true;
                    txtCode.BackColor = Color.FromArgb(245, 247, 250);
                    string next = productRepo.GetNextProductCode();
                    txtCode.Text = next;
                    txtBarcode.Text = next;
                    txtName.Clear(); txtPurchase.Clear(); txtSale.Clear(); txtMinStock.Text = "0";
                    cmbCategory.Enabled = true; chkCategory.Enabled = true;
                }
            };
            card.Controls.Add(chkEditExisting);
            txtCode.Leave += (s, e) =>
            {
                if (!chkEditExisting.Checked) return;
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
                // Edit mode: name + rates only
                txtBarcode.ReadOnly = true;
                cmbCategory.Enabled = false;
                chkCategory.Enabled = false;
            };
            y += 42;

            AddL(card, "Product Name", 0, y); txtName = AddT(card, 150, y, 300); txtName.KeyDown += Next; y += 42;
            AddL(card, "Barcode", 0, y); txtBarcode = AddT(card, 150, y, 220); txtBarcode.KeyDown += Next; y += 42;
            chkCategory = new CheckBox { Text = "Use Category", Font = UiHelper.NormalFont, Location = new Point(0, y), Size = new Size(140, 26), Checked = true };
            chkCategory.CheckedChanged += (s, e) => cmbCategory.Enabled = chkCategory.Checked;
            card.Controls.Add(chkCategory);
            cmbCategory = new ComboBox { Location = new Point(150, y), Size = new Size(300, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbCategory); card.Controls.Add(cmbCategory); y += 46;
            AddL(card, "Purchase Price", 0, y); txtPurchase = AddT(card, 150, y, 160); txtPurchase.KeyDown += Next; y += 42;
            AddL(card, "Sale Price", 0, y); txtSale = AddT(card, 150, y, 160); txtSale.KeyDown += Next; y += 42;
            AddL(card, "Min Stock", 0, y); txtMinStock = AddT(card, 150, y, 100); txtMinStock.Text = "0"; txtMinStock.KeyDown += Next; y += 56;

            Button btnSave = new Button { Text = "SAVE (F12)", Location = new Point(150, y), Size = new Size(140, 38) };
            UiHelper.StyleButton(btnSave); btnSave.Click += (s, e) => Save();
            Button btnClose = new Button { Text = "CLOSE (F4)", Location = new Point(304, y), Size = new Size(140, 38) };
            UiHelper.StyleButton(btnClose); btnClose.Click += (s, e) => this.Close();
            card.Controls.Add(btnSave); card.Controls.Add(btnClose);
        }

        private void Next(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectNextControl((Control)s, true, true, true, true); } }
        private void AddL(Control parent, string t, int x, int y) { parent.Controls.Add(new Label { Text = t, Font = UiHelper.NormalFont, Location = new Point(x, y + 4), Size = new Size(140, 22) }); }
        private TextBox AddT(Control parent, int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 28) }; UiHelper.StyleTextBox(t); parent.Controls.Add(t); return t; }

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Name required."); return; }
                decimal pur = 0, sale = 0; int min = 0;
                decimal.TryParse(txtPurchase.Text, out pur); decimal.TryParse(txtSale.Text, out sale); int.TryParse(txtMinStock.Text, out min);

                if (chkEditExisting.Checked && editingProductId.HasValue)
                {
                    productRepo.Update(editingProductId.Value, txtName.Text.Trim(), pur, sale, min, true);
                    MessageBox.Show("Product updated (name / rates).");
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
                    PurchasePrice = pur, SalePrice = sale, MinimumStock = min
                });
                MessageBox.Show("Saved!");
                string next = productRepo.GetNextProductCode();
                txtCode.Text = next; txtBarcode.Text = next; txtName.Clear(); txtPurchase.Clear(); txtSale.Clear(); txtMinStock.Text = "0"; txtName.Focus();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
