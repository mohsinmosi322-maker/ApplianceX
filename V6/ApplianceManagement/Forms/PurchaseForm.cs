using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class PurchaseForm : Form
    {
        private ProductRepository productRepo = new ProductRepository();
        private SupplierRepository supplierRepo = new SupplierRepository();
        private PurchaseRepository purchaseRepo = new PurchaseRepository();
        private List<PurchaseDetail> cart = new List<PurchaseDetail>();
        private Supplier selectedSupplier;
        private TextBox txtSearch, txtQty, txtDiscount, txtDiscAmt, txtPaid, txtTotal, txtNet;
        private ComboBox cmbSupplier;
        private DataGridView dgv;
        private ListBox lstSuggest;
        private bool calcBusy = false;

        public PurchaseForm()
        {
            InitializeComponent();
            var list = supplierRepo.GetAllActive();
            cmbSupplier.DataSource = list;
            cmbSupplier.DisplayMember = "SupplierName";
            if (list.Count > 0) { cmbSupplier.SelectedIndex = 0; selectedSupplier = list[0]; }
            txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "Purchase";
            this.Size = new Size(1100, 680);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F12) { txtDiscount.Focus(); txtDiscount.SelectAll(); }
            };

            Panel top = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = Color.White };
            top.Controls.Add(new Label { Text = "Invoice  AUTO", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(16, 12), AutoSize = true });
            top.Controls.Add(new Label { Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm"), Font = UiHelper.NormalFont, ForeColor = Color.FromArgb(110, 122, 136), Location = new Point(200, 14), AutoSize = true });
            top.Controls.Add(new Label { Text = "Supplier", Font = UiHelper.SmallFont, Location = new Point(430, 14), AutoSize = true });
            cmbSupplier = new ComboBox { Location = new Point(490, 10), Size = new Size(260, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbSupplier);
            cmbSupplier.SelectedIndexChanged += (s, e) => { if (cmbSupplier.SelectedItem != null) selectedSupplier = (Supplier)cmbSupplier.SelectedItem; };
            top.Controls.Add(cmbSupplier);

            top.Controls.Add(new Label { Text = "Search", Font = UiHelper.SmallFont, Location = new Point(16, 52), AutoSize = true });
            txtSearch = new TextBox { Location = new Point(70, 48), Size = new Size(320, 28) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) =>
            {
                string q = txtSearch.Text.Trim();
                if (q.Length < 2) { lstSuggest.Visible = false; return; }
                var list = productRepo.Search(q);
                lstSuggest.DataSource = null; lstSuggest.DataSource = list; lstSuggest.Visible = list.Count > 0;
                if (list.Count > 0) lstSuggest.SelectedIndex = 0;
            };
            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    if (lstSuggest.Visible && lstSuggest.SelectedItem != null) SelectSug();
                    else
                    {
                        var p = productRepo.GetByBarcode(txtSearch.Text.Trim()) ?? productRepo.GetByCode(txtSearch.Text.Trim());
                        if (p != null) { txtSearch.Tag = p; txtQty.Focus(); txtQty.SelectAll(); }
                        else MessageBox.Show("Product not found.");
                    }
                }
            };
            top.Controls.Add(txtSearch);
            lstSuggest = new ListBox { Location = new Point(70, 78), Size = new Size(320, 100), Visible = false };
            lstSuggest.Click += (s, e) => SelectSug();
            top.Controls.Add(lstSuggest);
            lstSuggest.BringToFront();
            top.Controls.Add(new Label { Text = "Qty", Font = UiHelper.SmallFont, Location = new Point(410, 52), AutoSize = true });
            txtQty = new TextBox { Location = new Point(440, 48), Size = new Size(64, 28), Text = "1" };
            UiHelper.StyleTextBox(txtQty);
            txtQty.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return; e.SuppressKeyPress = true;
                if (!(txtSearch.Tag is Product p)) return;
                int qty = 1; int.TryParse(txtQty.Text, out qty); if (qty < 1) qty = 1;
                var ex = cart.Find(x => x.ProductID == p.ProductID);
                if (ex != null) { ex.Quantity += qty; ex.Amount = ex.Quantity * ex.PurchasePrice; }
                else cart.Add(new PurchaseDetail { ProductID = p.ProductID, ProductCode = p.ProductCode, ProductName = p.ProductName, Quantity = qty, PurchasePrice = p.PurchasePrice, Amount = qty * p.PurchasePrice });
                RefreshGrid(); txtSearch.Clear(); txtSearch.Tag = null; txtQty.Text = "1"; txtSearch.Focus();
            };
            top.Controls.Add(txtQty);
            this.Controls.Add(top);

            Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.White };
            Button btnSave = new Button { Text = "SAVE  (F12)", Location = new Point(16, 8), Size = new Size(150, 36) };
            UiHelper.StyleButton(btnSave); btnSave.Click += (s, e) => Save();
            Button btnClose = new Button { Text = "CLOSE  (F4)", Location = new Point(180, 8), Size = new Size(150, 36) };
            UiHelper.StyleButton(btnClose); btnClose.Click += (s, e) => this.Close();
            bottom.Controls.Add(btnSave); bottom.Controls.Add(btnClose);
            this.Controls.Add(bottom);

            Panel tot = new Panel { Dock = DockStyle.Right, Width = 300, BackColor = Color.White };
            int y = 16;
            tot.Controls.Add(new Label { Text = "Totals", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeDark, Location = new Point(16, y), AutoSize = true }); y += 36;
            tot.Controls.Add(new Label { Text = "Total", Font = UiHelper.SmallFont, Location = new Point(16, y + 4), AutoSize = true });
            txtTotal = new TextBox { Location = new Point(110, y), Size = new Size(160, 28), Text = "0.00" };
            UiHelper.StyleTextBox(txtTotal); tot.Controls.Add(txtTotal);
            txtTotal.TextChanged += (s, e) => OnTotalChanged();
            txtTotal.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtDiscount.Focus(); txtDiscount.SelectAll(); } };
            y += 40;
            tot.Controls.Add(new Label { Text = "Disc %", Font = UiHelper.SmallFont, Location = new Point(16, y + 4), AutoSize = true });
            txtDiscount = new TextBox { Location = new Point(110, y), Size = new Size(160, 28), Text = "0" };
            UiHelper.StyleTextBox(txtDiscount); tot.Controls.Add(txtDiscount);
            txtDiscount.TextChanged += (s, e) => OnPctChanged();
            txtDiscount.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtDiscAmt.Focus(); txtDiscAmt.SelectAll(); } };
            y += 40;
            tot.Controls.Add(new Label { Text = "Discount", Font = UiHelper.SmallFont, Location = new Point(16, y + 4), AutoSize = true });
            txtDiscAmt = new TextBox { Location = new Point(110, y), Size = new Size(160, 28), Text = "0.00" };
            UiHelper.StyleTextBox(txtDiscAmt); tot.Controls.Add(txtDiscAmt);
            txtDiscAmt.TextChanged += (s, e) => OnAmtChanged();
            txtDiscAmt.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtNet.Focus(); txtNet.SelectAll(); } };
            y += 40;
            tot.Controls.Add(new Label { Text = "Net", Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeColor, Location = new Point(16, y + 2), AutoSize = true });
            txtNet = new TextBox { Location = new Point(110, y), Size = new Size(160, 28), Text = "0.00" };
            UiHelper.StyleTextBox(txtNet); tot.Controls.Add(txtNet);
            txtNet.TextChanged += (s, e) => OnNetChanged();
            txtNet.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtPaid.Text = txtNet.Text; txtPaid.Focus(); txtPaid.SelectAll(); } };
            y += 40;
            tot.Controls.Add(new Label { Text = "Paid", Font = UiHelper.SmallFont, Location = new Point(16, y + 4), AutoSize = true });
            txtPaid = new TextBox { Location = new Point(110, y), Size = new Size(160, 28), Text = "0.00" };
            UiHelper.StyleTextBox(txtPaid); tot.Controls.Add(txtPaid);
            txtPaid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Save(); } };
            this.Controls.Add(tot);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGrid(dgv);
            this.Controls.Add(dgv);
            dgv.BringToFront();
        }

        private void SelectSug()
        {
            if (lstSuggest.SelectedItem is Product p)
            { txtSearch.Text = p.ProductCode + " - " + p.ProductName; txtSearch.Tag = p; lstSuggest.Visible = false; txtQty.Text = "1"; txtQty.Focus(); txtQty.SelectAll(); }
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null; dgv.DataSource = cart;
            foreach (var h in new[] { "PurchaseDetailID", "PurchaseID", "ProductID", "Discount" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
            decimal baseTotal = 0; foreach (var i in cart) baseTotal += i.Amount;
            calcBusy = true;
            txtTotal.Text = baseTotal.ToString("0.00");
            decimal pct = 0; decimal.TryParse(txtDiscount.Text, out pct);
            if (pct < 0) pct = 0; if (pct > 100) pct = 100;
            decimal discAmt = Math.Round(baseTotal * pct / 100m, 2);
            txtDiscAmt.Text = discAmt.ToString("0.00");
            txtNet.Text = (baseTotal - discAmt).ToString("0.00");
            calcBusy = false;
        }

        private decimal ReadTotal()
        {
            decimal t = 0; decimal.TryParse(txtTotal.Text, out t);
            if (t < 0) t = 0; return t;
        }

        private void OnPctChanged()
        {
            if (calcBusy) return;
            calcBusy = true;
            decimal total = ReadTotal();
            decimal pct = 0; decimal.TryParse(txtDiscount.Text, out pct);
            if (pct < 0) pct = 0; if (pct > 100) pct = 100;
            decimal discAmt = Math.Round(total * pct / 100m, 2);
            txtDiscAmt.Text = discAmt.ToString("0.00");
            txtNet.Text = (total - discAmt).ToString("0.00");
            calcBusy = false;
        }

        private void OnAmtChanged()
        {
            if (calcBusy) return;
            calcBusy = true;
            decimal total = ReadTotal();
            decimal discAmt = 0; decimal.TryParse(txtDiscAmt.Text, out discAmt);
            if (discAmt < 0) discAmt = 0; if (discAmt > total) discAmt = total;
            decimal pct = total > 0 ? Math.Round(discAmt * 100m / total, 2) : 0;
            txtDiscount.Text = pct.ToString("0.##");
            txtNet.Text = (total - discAmt).ToString("0.00");
            calcBusy = false;
        }

        private void OnTotalChanged()
        {
            if (calcBusy) return;
            calcBusy = true;
            decimal total = ReadTotal();
            decimal discAmt = 0; decimal.TryParse(txtDiscAmt.Text, out discAmt);
            if (discAmt < 0) discAmt = 0; if (discAmt > total) discAmt = total;
            decimal pct = total > 0 ? Math.Round(discAmt * 100m / total, 2) : 0;
            txtDiscount.Text = pct.ToString("0.##");
            txtDiscAmt.Text = discAmt.ToString("0.00");
            txtNet.Text = (total - discAmt).ToString("0.00");
            calcBusy = false;
        }

        private void OnNetChanged()
        {
            if (calcBusy) return;
            calcBusy = true;
            decimal total = ReadTotal();
            decimal net = 0; decimal.TryParse(txtNet.Text, out net);
            if (net < 0) net = 0; if (net > total) net = total;
            decimal discAmt = Math.Round(total - net, 2);
            decimal pct = total > 0 ? Math.Round(discAmt * 100m / total, 2) : 0;
            txtDiscAmt.Text = discAmt.ToString("0.00");
            txtDiscount.Text = pct.ToString("0.##");
            calcBusy = false;
        }

        private void Save()
        {
            if (cart.Count == 0) { MessageBox.Show("Add products first."); return; }
            if (selectedSupplier == null) { MessageBox.Show("Select supplier (add from Settings)."); return; }
            if (MessageBox.Show("Save this purchase?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            try
            {
                decimal total = ReadTotal();
                decimal pct = 0, discAmt = 0, paid = 0, net = 0;
                decimal.TryParse(txtDiscount.Text, out pct);
                decimal.TryParse(txtDiscAmt.Text, out discAmt);
                decimal.TryParse(txtPaid.Text, out paid);
                decimal.TryParse(txtNet.Text, out net);
                discAmt = Math.Round(discAmt, 2);
                if (net <= 0) net = Math.Round(total - discAmt, 2);
                purchaseRepo.SavePurchase(new PurchaseHeader
                {
                    PurchaseDate = DateTime.Now, SupplierID = selectedSupplier.SupplierID,
                    TotalAmount = total, Discount = discAmt, NetAmount = net,
                    PaidAmount = paid, BalanceAmount = net - paid, Details = cart
                });
                MessageBox.Show("Purchase saved!");
                this.Tag = "NOSAVECONFIRM";
                if (MainForm.Instance != null)
                    MainForm.Instance.OpenChild(new PurchaseForm(), "PURCHASE");
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }
    }
}
