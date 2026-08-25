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
        private CheckBox chkCategory;

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
            this.Text = "New Item"; this.Size = new Size(520, 460); this.MinimumSize = new Size(520, 460);
            this.BackColor = UiHelper.BgColor; this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F12) Save();  };

            int y = 20;
            AddL("Product Name:", 20, y); txtName = AddT(160, y, 280); txtName.KeyDown += Next; y += 38;
            AddL("Product Code:", 20, y); txtCode = AddT(160, y, 150); txtCode.ReadOnly = true; txtCode.BackColor = Color.FromArgb(240, 240, 240); y += 38;
            AddL("Barcode:", 20, y); txtBarcode = AddT(160, y, 200); txtBarcode.KeyDown += Next; y += 38;
            chkCategory = new CheckBox { Text = "Use Category", Font = UiHelper.NormalFont, Location = new Point(20, y), Size = new Size(130, 25), Checked = true };
            chkCategory.CheckedChanged += (s, e) => cmbCategory.Enabled = chkCategory.Checked;
            this.Controls.Add(chkCategory);
            cmbCategory = new ComboBox { Location = new Point(160, y), Size = new Size(280, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbCategory); this.Controls.Add(cmbCategory); y += 42;
            AddL("Purchase Price:", 20, y); txtPurchase = AddT(160, y, 150); txtPurchase.KeyDown += Next; y += 38;
            AddL("Sale Price:", 20, y); txtSale = AddT(160, y, 150); txtSale.KeyDown += Next; y += 38;
            AddL("Min Stock Level:", 20, y); txtMinStock = AddT(160, y, 100); txtMinStock.Text = "0"; txtMinStock.KeyDown += Next; y += 50;

            Button btnSave = new Button { Text = "SAVE (F12)", Location = new Point(160, y), Size = new Size(130, 34) };
            UiHelper.StyleButton(btnSave); btnSave.Click += (s, e) => Save();
            Button btnClose = new Button { Text = "CLOSE", Location = new Point(310, y), Size = new Size(130, 34) };
            UiHelper.StyleButton(btnClose); btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnSave); this.Controls.Add(btnClose);
        }

        private void Next(object s, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectNextControl((Control)s, true, true, true, true); } }
        private void AddL(string t, int x, int y) { this.Controls.Add(new Label { Text = t, Font = UiHelper.NormalFont, Location = new Point(x, y + 3), Size = new Size(130, 22) }); }
        private TextBox AddT(int x, int y, int w) { var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 26) }; UiHelper.StyleTextBox(t); this.Controls.Add(t); return t; }

        private void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Name required."); return; }
                if (productRepo.ExistsCode(txtCode.Text.Trim())) { MessageBox.Show("Code exists."); return; }
                decimal pur = 0, sale = 0; int min = 0;
                decimal.TryParse(txtPurchase.Text, out pur); decimal.TryParse(txtSale.Text, out sale); int.TryParse(txtMinStock.Text, out min);
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
