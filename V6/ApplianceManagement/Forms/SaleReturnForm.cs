using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    /// <summary>Same UX as Sale (search, cart, discount footer) but increases stock on save.</summary>
    public partial class SaleReturnForm : Form
    {
        private ProductRepository productRepo = new ProductRepository();
        private SaleRepository saleRepo = new SaleRepository();
        private List<SaleDetail> cart = new List<SaleDetail>();
        private Product selectedProduct;
        private TextBox txtSearch, txtQty, txtDiscount, txtDiscAmt, txtPaid, txtTotal, txtNet;
        private DataGridView dgv;
        private ListBox lstSuggest;
        private bool calcBusy = false;

        public SaleReturnForm()
        {
            InitializeComponent();
            txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "Sale Return";
            this.Size = new Size(1100, 680);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F12) { txtDiscount.Focus(); txtDiscount.SelectAll(); }
                if (e.KeyCode == Keys.F9) { ShowHistory(); e.Handled = true; }
                if (e.KeyCode == Keys.F8 && dgv.SelectedRows.Count > 0)
                {
                    int i = dgv.SelectedRows[0].Index;
                    if (i >= 0 && i < cart.Count) { cart.RemoveAt(i); RefreshGrid(); }
                }
                if ((e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) && cart.Count > 0)
                {
                    int idx = dgv.SelectedRows.Count > 0 ? dgv.SelectedRows[0].Index : 0;
                    idx += e.KeyCode == Keys.Up ? -1 : 1;
                    if (idx < 0) idx = 0; if (idx >= cart.Count) idx = cart.Count - 1;
                    dgv.ClearSelection();
                    if (idx < dgv.Rows.Count) { dgv.Rows[idx].Selected = true; dgv.CurrentCell = dgv.Rows[idx].Cells[0]; }
                    e.Handled = true;
                }
            };

            this.Controls.Add(UiHelper.CreateFormBanner(
                "SALE RETURN",
                "Customer returns  \u2022  Stock will INCREASE  \u2022  Discount & paid  \u2022  F9 history",
                FormAccent.SaleReturn, FormAccent.SaleReturnDark));

            Panel top = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.White };
            top.Controls.Add(new Label { Text = "RETURN  AUTO", Font = UiHelper.HeaderFont, ForeColor = FormAccent.SaleReturnDark, Location = new Point(16, 10), AutoSize = true });
            top.Controls.Add(new Label { Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm"), Font = UiHelper.NormalFont, ForeColor = Color.FromArgb(110, 122, 136), Location = new Point(200, 12), AutoSize = true });
            top.Controls.Add(new Label { Text = "Stock will INCREASE", Font = UiHelper.NormalFont, ForeColor = FormAccent.Purchase, Location = new Point(430, 12), AutoSize = true });
            top.Controls.Add(new Label { Text = "Search", Font = UiHelper.SmallFont, Location = new Point(16, 50), AutoSize = true });
            txtSearch = new TextBox { Location = new Point(70, 46), Size = new Size(320, 28) };
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
                    else
                    {
                        var p = productRepo.GetByBarcode(txtSearch.Text.Trim()) ?? productRepo.GetByCode(txtSearch.Text.Trim());
                        if (p != null) { SetSel(p); txtQty.Focus(); txtQty.SelectAll(); }
                        else MessageBox.Show("Product not found.");
                    }
                }
                if (e.KeyCode == Keys.Down && lstSuggest.Visible) { lstSuggest.Focus(); e.Handled = true; }
                if (e.KeyCode == Keys.F9) { ShowHistory(); e.Handled = true; }
            };
            top.Controls.Add(txtSearch);
            top.Controls.Add(new Label { Text = "Qty", Font = UiHelper.SmallFont, Location = new Point(410, 50), AutoSize = true });
            txtQty = new TextBox { Location = new Point(440, 46), Size = new Size(64, 28), Text = "1" };
            UiHelper.StyleTextBox(txtQty);
            txtQty.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                if (selectedProduct == null) return;
                Product p = selectedProduct;
                int qty = 1; int.TryParse(txtQty.Text, out qty); if (qty < 1) qty = 1;
                decimal unitPrice = p.UnitSalePrice;
                // use 'line' not 'x' — avoids CS0136 with footer int x below
                var ex = cart.Find(line => line.ProductID == p.ProductID);
                if (ex != null) { ex.Quantity += qty; ex.Amount = ex.Quantity * ex.SalePrice; }
                else cart.Add(new SaleDetail { ProductID = p.ProductID, ProductCode = p.ProductCode, ProductName = p.ProductName, Quantity = qty, SalePrice = unitPrice, Amount = qty * unitPrice });
                RefreshGrid(); txtSearch.Clear(); selectedProduct = null; txtQty.Text = "1"; lstSuggest.Visible = false; txtSearch.Focus();
            };
            top.Controls.Add(txtQty);
            this.Controls.Add(top);

            lstSuggest = new ListBox { Location = new Point(70, 140), Size = new Size(420, 160), Visible = false, Font = UiHelper.NormalFont, IntegralHeight = false };
            lstSuggest.Click += (s, e) => SelectSug();
            this.Controls.Add(lstSuggest);

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 78, BackColor = Color.FromArgb(253, 242, 233) };
            int fx = 16;
            foot.Controls.Add(new Label { Text = "Total", Font = UiHelper.SmallFont, Location = new Point(fx, 10), AutoSize = true });
            txtTotal = Box(foot, fx, 32, 110); txtTotal.Text = "0.00"; fx += 126;
            foot.Controls.Add(new Label { Text = "Disc %", Font = UiHelper.SmallFont, Location = new Point(fx, 10), AutoSize = true });
            txtDiscount = Box(foot, fx, 32, 70); txtDiscount.Text = "0"; txtDiscount.TextChanged += (s, e) => OnPct(); fx += 86;
            foot.Controls.Add(new Label { Text = "Discount", Font = UiHelper.SmallFont, Location = new Point(fx, 10), AutoSize = true });
            txtDiscAmt = Box(foot, fx, 32, 110); txtDiscAmt.Text = "0.00"; txtDiscAmt.TextChanged += (s, e) => OnAmt(); fx += 126;
            foot.Controls.Add(new Label { Text = "Net", Font = UiHelper.SmallFont, Location = new Point(fx, 10), AutoSize = true });
            txtNet = Box(foot, fx, 32, 120); txtNet.Text = "0.00"; txtNet.ForeColor = FormAccent.SaleReturn; fx += 136;
            foot.Controls.Add(new Label { Text = "Paid", Font = UiHelper.SmallFont, Location = new Point(fx, 10), AutoSize = true });
            txtPaid = Box(foot, fx, 32, 110); txtPaid.Text = "0.00";
            Button btnSave = new Button { Text = "SAVE RETURN (F12)", Size = new Size(160, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Button btnClose = new Button { Text = "CLOSE (F4)", Size = new Size(120, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            UiHelper.StyleAccentButton(btnSave, FormAccent.SaleReturn, FormAccent.SaleReturnDark);
            UiHelper.StyleAccentButton(btnClose, FormAccent.SaleReturnDark, FormAccent.SaleReturn);
            btnSave.Click += (s, e) => Save();
            btnClose.Click += (s, e) => this.Close();
            foot.Controls.Add(btnSave); foot.Controls.Add(btnClose);
            foot.Resize += (s, e) =>
            {
                btnClose.Location = new Point(foot.Width - 16 - btnClose.Width, 22);
                btnSave.Location = new Point(btnClose.Left - 10 - btnSave.Width, 22);
            };
            this.Controls.Add(foot);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.SaleReturn);
            this.Controls.Add(dgv);
            dgv.BringToFront();
            lstSuggest.BringToFront();
        }

        private TextBox Box(Control p, int posX, int y, int w)
        {
            var t = new TextBox { Location = new Point(posX, y), Size = new Size(w, 28) };
            UiHelper.StyleTextBox(t);
            p.Controls.Add(t);
            return t;
        }

        private void SetSel(Product p) { selectedProduct = p; txtSearch.Text = p.ProductCode + " - " + p.ProductName; lstSuggest.Visible = false; }
        private void SelectSug() { if (lstSuggest.SelectedItem is Product p) { SetSel(p); txtQty.Text = "1"; txtQty.Focus(); txtQty.SelectAll(); } }

        private void ShowHistory()
        {
            Product p = selectedProduct;
            if (p == null)
            {
                string q = txtSearch.Text.Trim();
                if (q.Contains(" - ")) q = q.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();
                if (q.Length > 0) p = productRepo.GetByBarcode(q) ?? productRepo.GetByCode(q);
            }
            if (p == null) { MessageBox.Show("Select product first, then F9."); return; }
            using (var f = new ProductHistoryForm(p, true)) f.ShowDialog(this);
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null; dgv.DataSource = cart;
            foreach (var h in new[] { "SaleDetailID", "SaleID", "ProductID", "Discount" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
            decimal total = 0; foreach (var i in cart) total += i.Amount;
            calcBusy = true;
            txtTotal.Text = total.ToString("0.00");
            decimal pct = 0; decimal.TryParse(txtDiscount.Text, out pct);
            decimal disc = Math.Round(total * pct / 100m, 2);
            txtDiscAmt.Text = disc.ToString("0.00");
            txtNet.Text = (total - disc).ToString("0.00");
            calcBusy = false;
        }

        private void OnPct()
        {
            if (calcBusy) return; calcBusy = true;
            decimal total = 0; decimal.TryParse(txtTotal.Text, out total);
            decimal pct = 0; decimal.TryParse(txtDiscount.Text, out pct);
            decimal disc = Math.Round(total * pct / 100m, 2);
            txtDiscAmt.Text = disc.ToString("0.00");
            txtNet.Text = (total - disc).ToString("0.00");
            calcBusy = false;
        }

        private void OnAmt()
        {
            if (calcBusy) return; calcBusy = true;
            decimal total = 0; decimal.TryParse(txtTotal.Text, out total);
            decimal disc = 0; decimal.TryParse(txtDiscAmt.Text, out disc);
            decimal pct = total > 0 ? Math.Round(disc * 100m / total, 2) : 0;
            txtDiscount.Text = pct.ToString("0.##");
            txtNet.Text = (total - disc).ToString("0.00");
            calcBusy = false;
        }

        private void Save()
        {
            if (cart.Count == 0) { MessageBox.Show("Add products first."); return; }
            if (MessageBox.Show("Save sale return? Stock will increase.", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try
            {
                foreach (var line in cart)
                    saleRepo.SaveSaleReturn(line.ProductID, line.Quantity, "Return line");
                MessageBox.Show("Return saved. Stock updated.");
                cart.Clear(); RefreshGrid();
                txtSearch.Clear(); selectedProduct = null; txtPaid.Text = "0.00"; txtSearch.Focus();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
