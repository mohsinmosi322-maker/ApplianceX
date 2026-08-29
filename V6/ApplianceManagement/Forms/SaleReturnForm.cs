using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class SaleReturnForm : Form
    {
        private ProductRepository productRepo = new ProductRepository();
        private SaleRepository saleRepo = new SaleRepository();
        private TextBox txtSearch, txtQty, txtReason;
        private ListBox lstSuggest;
        private Label lblProduct;

        public SaleReturnForm()
        {
            InitializeComponent();
            txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "Sale Return";
            this.Size = new Size(560, 360);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F12) Save(); };

            Panel card = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(24) };
            this.Controls.Add(card);

            int y = 16;
            card.Controls.Add(new Label { Text = "Return product to stock", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(0, y), AutoSize = true });
            y += 36;
            card.Controls.Add(new Label { Text = "Search", Font = UiHelper.SmallFont, Location = new Point(0, y), AutoSize = true });
            txtSearch = new TextBox { Location = new Point(80, y - 4), Size = new Size(320, 28) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) =>
            {
                string q = txtSearch.Text.Trim();
                if (q.Length < 2) { lstSuggest.Visible = false; return; }
                var list = productRepo.Search(q);
                lstSuggest.DataSource = null; lstSuggest.DataSource = list;
                lstSuggest.Visible = list.Count > 0;
                if (list.Count > 0) lstSuggest.SelectedIndex = 0;
                lstSuggest.BringToFront();
            };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (lstSuggest.Visible && lstSuggest.SelectedItem != null) SelectSug();
                }
                if (e.KeyCode == Keys.Down && lstSuggest.Visible) lstSuggest.Focus();
            };
            card.Controls.Add(txtSearch);
            y += 36;
            lstSuggest = new ListBox { Location = new Point(80, y - 8), Size = new Size(320, 100), Visible = false };
            lstSuggest.Click += (s, e) => SelectSug();
            card.Controls.Add(lstSuggest);

            lblProduct = new Label { Text = "No product selected", Font = UiHelper.NormalFont, Location = new Point(0, y + 100), Size = new Size(480, 24) };
            card.Controls.Add(lblProduct);
            y += 130;

            card.Controls.Add(new Label { Text = "Qty", Font = UiHelper.SmallFont, Location = new Point(0, y), AutoSize = true });
            txtQty = new TextBox { Location = new Point(80, y - 4), Size = new Size(80, 28), Text = "1" };
            UiHelper.StyleTextBox(txtQty);
            card.Controls.Add(txtQty);
            y += 40;

            card.Controls.Add(new Label { Text = "Reason", Font = UiHelper.SmallFont, Location = new Point(0, y), AutoSize = true });
            txtReason = new TextBox { Location = new Point(80, y - 4), Size = new Size(320, 28) };
            UiHelper.StyleTextBox(txtReason);
            card.Controls.Add(txtReason);
            y += 48;

            Button btnSave = new Button { Text = "SAVE RETURN (F12)", Location = new Point(80, y), Size = new Size(180, 36) };
            UiHelper.StyleButton(btnSave);
            btnSave.Click += (s, e) => Save();
            Button btnClose = new Button { Text = "CLOSE (F4)", Location = new Point(280, y), Size = new Size(120, 36) };
            UiHelper.StyleButton(btnClose);
            btnClose.Click += (s, e) => this.Close();
            card.Controls.Add(btnSave);
            card.Controls.Add(btnClose);
        }

        private void SelectSug()
        {
            if (lstSuggest.SelectedItem is Product p)
            {
                txtSearch.Text = p.ProductCode + " - " + p.ProductName;
                txtSearch.Tag = p;
                lstSuggest.Visible = false;
                lblProduct.Text = p.ProductName + "  |  Stock: " + p.CurrentStock + "  |  Sale: " + p.SalePrice.ToString("0.00");
                txtQty.Focus();
            }
        }

        private void Save()
        {
            if (!(txtSearch.Tag is Product p))
            {
                MessageBox.Show("Select a product first.");
                return;
            }
            int qty = 1;
            int.TryParse(txtQty.Text, out qty);
            if (qty < 1) { MessageBox.Show("Qty must be at least 1."); return; }
            if (MessageBox.Show("Return " + qty + " x " + p.ProductName + " to stock?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;
            try
            {
                saleRepo.SaveSaleReturn(p.ProductID, qty, txtReason.Text.Trim());
                MessageBox.Show("Return saved. Stock increased by " + qty + ".");
                txtSearch.Clear(); txtSearch.Tag = null; txtQty.Text = "1"; txtReason.Clear();
                lblProduct.Text = "No product selected";
                txtSearch.Focus();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
