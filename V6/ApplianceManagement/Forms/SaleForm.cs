using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class SaleForm : Form
    {
        private ProductRepository productRepo = new ProductRepository();
        private CustomerRepository customerRepo = new CustomerRepository();
        private SaleRepository saleRepo = new SaleRepository();
        private List<SaleDetail> cart = new List<SaleDetail>();
        private Customer walkIn;
        private TextBox txtSearch, txtQty, txtDiscount, txtDiscAmt, txtPaid, txtTotal, txtNet, txtBalance;
        private DataGridView dgv;
        private ListBox lstSuggest;
        private bool skipCloseConfirm = false;
        private bool calcBusy = false;
        private decimal cartBaseTotal = 0;

        public SaleForm()
        {
            walkIn = customerRepo.GetWalkInCustomer();
            InitializeComponent();
            txtSearch.Focus();
        }

        private void InitializeComponent()
        {
            this.Text = "Sale";
            this.Size = new Size(1020, 640);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            UiHelper.AttachF4Close(this);
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F12) { txtDiscount.Focus(); txtDiscount.SelectAll(); }
                if (e.KeyCode == Keys.F8 && dgv.SelectedRows.Count > 0)
                {
                    int i = dgv.SelectedRows[0].Index;
                    if (i >= 0 && i < cart.Count) { cart.RemoveAt(i); RefreshGrid(); }
                }
            };

            Panel top = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.White };
            top.Controls.Add(new Label { Text = "Invoice: AUTO", Font = UiHelper.HeaderFont, Location = new Point(15, 12), Size = new Size(150, 22) });
            top.Controls.Add(new Label { Text = "Date: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), Font = UiHelper.HeaderFont, Location = new Point(180, 12), Size = new Size(220, 22) });
            top.Controls.Add(new Label { Text = "Customer: Walk-in Customer", Font = UiHelper.HeaderFont, Location = new Point(420, 12), Size = new Size(300, 22) });
            top.Controls.Add(new Label { Text = "Search:", Font = UiHelper.NormalFont, Location = new Point(15, 50), Size = new Size(55, 22) });
            txtSearch = new TextBox { Location = new Point(75, 47), Size = new Size(280, 26) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) =>
            {
                string q = txtSearch.Text.Trim();
                if (q.Length < 2) { lstSuggest.Visible = false; return; }
                var list = productRepo.Search(q);
                lstSuggest.DataSource = null; lstSuggest.DataSource = list;
                lstSuggest.Visible = list.Count > 0;
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
                if (e.KeyCode == Keys.Down && lstSuggest.Visible) lstSuggest.Focus();
            };
            top.Controls.Add(txtSearch);
            lstSuggest = new ListBox { Location = new Point(75, 74), Size = new Size(280, 90), Visible = false };
            lstSuggest.Click += (s, e) => SelectSug();
            top.Controls.Add(lstSuggest);
            top.Controls.Add(new Label { Text = "Qty:", Font = UiHelper.NormalFont, Location = new Point(370, 50), Size = new Size(35, 22) });
            txtQty = new TextBox { Location = new Point(410, 47), Size = new Size(60, 26), Text = "1" };
            UiHelper.StyleTextBox(txtQty);
            txtQty.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                if (!(txtSearch.Tag is Product p)) return;
                int qty = 1; int.TryParse(txtQty.Text, out qty); if (qty < 1) qty = 1;
                if (qty > p.CurrentStock) { MessageBox.Show("Insufficient stock. Available: " + p.CurrentStock); return; }
                var ex = cart.Find(x => x.ProductID == p.ProductID);
                if (ex != null) { if (ex.Quantity + qty > p.CurrentStock) { MessageBox.Show("Insufficient stock."); return; } ex.Quantity += qty; ex.Amount = ex.Quantity * ex.SalePrice; }
                else cart.Add(new SaleDetail { ProductID = p.ProductID, ProductCode = p.ProductCode, ProductName = p.ProductName, Quantity = qty, SalePrice = p.SalePrice, Amount = qty * p.SalePrice });
                RefreshGrid(); txtSearch.Clear(); txtSearch.Tag = null; txtQty.Text = "1"; txtSearch.Focus();
            };
            top.Controls.Add(txtQty);
            this.Controls.Add(top);

            dgv = new DataGridView { Location = new Point(15, 100), Size = new Size(980, 320) };
            UiHelper.StyleGrid(dgv);
            this.Controls.Add(dgv);

            Panel tot = new Panel { Location = new Point(560, 430), Size = new Size(440, 155), BackColor = Color.White };
            int y = 10;
            tot.Controls.Add(new Label { Text = "Total:", Font = UiHelper.NormalFont, Location = new Point(15, y + 3), Size = new Size(80, 22) });
            txtTotal = new TextBox { Location = new Point(100, y), Size = new Size(150, 26), Text = "0.00" };
            UiHelper.StyleTextBox(txtTotal);
            txtTotal.TextChanged += (s, e) => OnTotalChanged();
            txtTotal.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtDiscount.Focus(); txtDiscount.SelectAll(); } };
            tot.Controls.Add(txtTotal);
            y += 30;
            tot.Controls.Add(new Label { Text = "Disc %:", Font = UiHelper.NormalFont, Location = new Point(15, y + 3), Size = new Size(55, 22) });
            txtDiscount = new TextBox { Location = new Point(75, y), Size = new Size(55, 26), Text = "0" };
            UiHelper.StyleTextBox(txtDiscount);
            txtDiscount.TextChanged += (s, e) => OnPctChanged();
            txtDiscount.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtDiscAmt.Focus(); txtDiscAmt.SelectAll(); } };
            tot.Controls.Add(txtDiscount);
            tot.Controls.Add(new Label { Text = "Rounding:", Font = UiHelper.NormalFont, Location = new Point(140, y + 3), Size = new Size(75, 22) });
            txtDiscAmt = new TextBox { Location = new Point(220, y), Size = new Size(100, 26), Text = "0.00" };
            UiHelper.StyleTextBox(txtDiscAmt);
            txtDiscAmt.TextChanged += (s, e) => OnAmtChanged();
            txtDiscAmt.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtNet.Focus(); txtNet.SelectAll(); } };
            tot.Controls.Add(txtDiscAmt);
            y += 30;
            tot.Controls.Add(new Label { Text = "Net:", Font = UiHelper.NormalFont, Location = new Point(15, y + 3), Size = new Size(80, 22) });
            txtNet = new TextBox { Location = new Point(100, y), Size = new Size(150, 26), Text = "0.00" };
            UiHelper.StyleTextBox(txtNet);
            txtNet.TextChanged += (s, e) => OnNetChanged();
            txtNet.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtPaid.Text = txtNet.Text; txtPaid.Focus(); txtPaid.SelectAll(); } };
            tot.Controls.Add(txtNet);
            y += 30;
            tot.Controls.Add(new Label { Text = "Paid:", Font = UiHelper.NormalFont, Location = new Point(15, y + 3), Size = new Size(80, 22) });
            txtPaid = new TextBox { Location = new Point(100, y), Size = new Size(150, 26), Text = "0.00" };
            UiHelper.StyleTextBox(txtPaid);
            txtPaid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Save(); } };
            tot.Controls.Add(txtPaid);
            this.Controls.Add(tot);

            Button btnSave = new Button { Text = "SAVE (F12)", Location = new Point(560, 590), Size = new Size(150, 36) };
            UiHelper.StyleButton(btnSave); btnSave.Click += (s, e) => Save();
            Button btnClose = new Button { Text = "CLOSE (F4)", Location = new Point(730, 590), Size = new Size(150, 36) };
            UiHelper.StyleButton(btnClose); btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnSave); this.Controls.Add(btnClose);
        }

        private void SelectSug()
        {
            if (lstSuggest.SelectedItem is Product p)
            {
                txtSearch.Text = p.ProductCode + " - " + p.ProductName;
                txtSearch.Tag = p; lstSuggest.Visible = false; txtQty.Text = "1"; txtQty.Focus(); txtQty.SelectAll();
            }
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null; dgv.DataSource = cart;
            foreach (var h in new[] { "SaleDetailID", "SaleID", "ProductID", "Discount" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
            cartBaseTotal = 0;
            foreach (var i in cart) cartBaseTotal += i.Amount;
            calcBusy = true;
            txtTotal.Text = cartBaseTotal.ToString("0.00");
            decimal pct = 0; decimal.TryParse(txtDiscount.Text, out pct);
            if (pct < 0) pct = 0; if (pct > 100) pct = 100;
            decimal discAmt = Math.Round(cartBaseTotal * pct / 100m, 2);
            txtDiscAmt.Text = discAmt.ToString("0.00");
            txtNet.Text = (cartBaseTotal - discAmt).ToString("0.00");
            calcBusy = false;
        }

        private decimal ReadTotal()
        {
            decimal t = 0;
            decimal.TryParse(txtTotal.Text, out t);
            if (t < 0) t = 0;
            return t;
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
            if (discAmt < 0) discAmt = 0;
            if (discAmt > total) discAmt = total;
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
            if (discAmt < 0) discAmt = 0;
            if (discAmt > total) discAmt = total;
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
            if (net < 0) net = 0;
            if (net > total) net = total;
            decimal discAmt = Math.Round(total - net, 2);
            decimal pct = total > 0 ? Math.Round(discAmt * 100m / total, 2) : 0;
            txtDiscAmt.Text = discAmt.ToString("0.00");
            txtDiscount.Text = pct.ToString("0.##");
            calcBusy = false;
        }

        private void Save()
        {
            if (cart.Count == 0) { MessageBox.Show("Add products first."); return; }
            if (walkIn == null) { MessageBox.Show("Walk-in Customer missing."); return; }

            decimal total = ReadTotal();
            decimal pct = 0, discAmt = 0, paid = 0, net = 0;
            decimal.TryParse(txtDiscount.Text, out pct);
            decimal.TryParse(txtDiscAmt.Text, out discAmt);
            decimal.TryParse(txtPaid.Text, out paid);
            decimal.TryParse(txtNet.Text, out net);
            if (pct < 0) pct = 0; if (pct > 100) pct = 100;
            discAmt = Math.Round(discAmt, 2);
            if (net <= 0) net = Math.Round(total - discAmt, 2);

            string role = MainForm.Instance != null ? MainForm.Instance.CurrentUser.Role : "User";
            decimal maxDisc = UiHelper.GetMaxDiscount(role);
            if (maxDisc > 0 && pct > maxDisc)
            {
                MessageBox.Show("Maximum allowed discount is " + maxDisc.ToString("0.##") + "% for " + role + ".");
                txtDiscount.Focus();
                return;
            }

            if (MessageBox.Show("Save this sale?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) != DialogResult.Yes) return;
            try
            {
                var sale = new SaleHeader
                {
                    SaleDate = DateTime.Now,
                    CustomerID = walkIn.CustomerID,
                    CustomerName = walkIn.CustomerName,
                    TotalAmount = total,
                    Discount = discAmt,
                    NetAmount = net,
                    PaidAmount = paid,
                    BalanceAmount = net - paid,
                    Details = new List<SaleDetail>(cart)
                };
                saleRepo.SaveSale(sale);
                MessageBox.Show("Sale saved successfully!\nInvoice: " + sale.InvoiceNo, "Success");

                if (UiHelper.IsPrintAllowed())
                {
                    try { BillPrinter.PrintSaleBill(sale); }
                    catch (Exception pex)
                    {
                        AppLog.Error("Print failed", pex);
                        MessageBox.Show("Print failed: " + pex.Message);
                    }
                }

                this.Tag = "NOSAVECONFIRM";
                if (MainForm.Instance != null)
                    MainForm.Instance.OpenChild(new SaleForm(), "SALE");
                this.Close();
            }
            catch (Exception ex)
            {
                AppLog.Error("Sale save UI error", ex);
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
