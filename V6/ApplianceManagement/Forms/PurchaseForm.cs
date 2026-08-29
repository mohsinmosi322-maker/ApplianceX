using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    /// <summary>
    /// Qty = PACKS. Stock += packs × PackSize. Line price = full pack PurchasePrice.
    /// </summary>
    public partial class PurchaseForm : Form
    {
        private ProductRepository productRepo = new ProductRepository();
        private SupplierRepository supplierRepo = new SupplierRepository();
        private PurchaseService purchaseService = new PurchaseService();
        private PurchaseRepository purchaseRepo = new PurchaseRepository();
        private List<PurchaseDetail> cart = new List<PurchaseDetail>();
        private Supplier selectedSupplier;
        private Product selectedProduct;
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
            this.KeyDown += Form_KeyDown;

            Panel top = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.White };
            top.Controls.Add(new Label { Text = "Invoice  AUTO", Font = UiHelper.HeaderFont, ForeColor = FormAccent.PurchaseDark, Location = new Point(16, 12), AutoSize = true });
            top.Controls.Add(new Label { Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm"), Font = UiHelper.NormalFont, ForeColor = Color.FromArgb(110, 122, 136), Location = new Point(200, 14), AutoSize = true });
            top.Controls.Add(new Label { Text = "Supplier", Font = UiHelper.SmallFont, Location = new Point(430, 14), AutoSize = true });
            cmbSupplier = new ComboBox { Location = new Point(490, 10), Size = new Size(260, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            UiHelper.StyleComboBox(cmbSupplier);
            cmbSupplier.SelectedIndexChanged += (s, e) =>
            {
                if (cmbSupplier.SelectedItem != null)
                    selectedSupplier = (Supplier)cmbSupplier.SelectedItem;
            };
            top.Controls.Add(cmbSupplier);

            top.Controls.Add(new Label { Text = "Search", Font = UiHelper.SmallFont, Location = new Point(16, 52), AutoSize = true });
            txtSearch = new TextBox { Location = new Point(70, 48), Size = new Size(320, 28) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => ShowSuggestions();
            txtSearch.KeyDown += Search_KeyDown;
            top.Controls.Add(txtSearch);

            top.Controls.Add(new Label { Text = "Packs", Font = UiHelper.SmallFont, Location = new Point(400, 52), AutoSize = true });
            txtQty = new TextBox { Location = new Point(445, 48), Size = new Size(64, 28), Text = "1" };
            UiHelper.StyleTextBox(txtQty);
            txtQty.KeyDown += Qty_KeyDown;
            top.Controls.Add(txtQty);
            top.Controls.Add(new Label
            {
                Text = "Enter add  F9 history  F12 disc  (qty = packs)",
                Font = UiHelper.SmallFont,
                ForeColor = Color.FromArgb(140, 150, 160),
                Location = new Point(520, 52),
                AutoSize = true
            });
            this.Controls.Add(top);

            this.Controls.Add(UiHelper.CreateFormBanner(
                "PURCHASE",
                "Qty = packs  ·  Stock += packs × pack size  ·  Full pack price  ·  F9 history",
                FormAccent.Purchase, FormAccent.PurchaseDark));

            lstSuggest = new ListBox
            {
                Location = new Point(70, 140),
                Size = new Size(420, 160),
                Visible = false,
                Font = UiHelper.NormalFont,
                IntegralHeight = false
            };
            lstSuggest.Click += (s, e) => SelectSug();
            lstSuggest.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectSug(); }
            };
            this.Controls.Add(lstSuggest);

            this.Controls.Add(BuildTotalsFooter());

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.Purchase);
            this.Controls.Add(dgv);
            dgv.BringToFront();
            lstSuggest.BringToFront();
        }

        private void Form_KeyDown(object s, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12) { txtDiscount.Focus(); txtDiscount.SelectAll(); e.Handled = true; }
            if (e.KeyCode == Keys.F9) { ShowProductHistory(); e.Handled = true; }
        }

        private void Search_KeyDown(object s, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (lstSuggest.Visible && lstSuggest.SelectedItem != null)
                    SelectSug();
                else
                {
                    var p = productRepo.GetByBarcode(txtSearch.Text.Trim())
                         ?? productRepo.GetByCode(txtSearch.Text.Trim());
                    if (p != null)
                    {
                        SetSelected(p);
                        txtQty.Focus();
                        txtQty.SelectAll();
                    }
                    else MessageBox.Show("Product not found.");
                }
            }
            if (e.KeyCode == Keys.Down && lstSuggest.Visible) { lstSuggest.Focus(); e.Handled = true; }
            if (e.KeyCode == Keys.Escape) lstSuggest.Visible = false;
            if (e.KeyCode == Keys.F9) { ShowProductHistory(); e.Handled = true; }
        }

        private void Qty_KeyDown(object s, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (selectedProduct == null) return;
            Product p = selectedProduct;
            int packs = 1;
            int.TryParse(txtQty.Text, out packs);
            if (packs < 1) packs = 1;

            decimal packPrice = p.PurchasePrice;
            var ex = cart.Find(line => line.ProductID == p.ProductID);
            if (ex != null)
            {
                ex.Quantity += packs;
                ex.Amount = ex.Quantity * ex.PurchasePrice;
            }
            else
            {
                cart.Add(new PurchaseDetail
                {
                    ProductID = p.ProductID,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    Quantity = packs,
                    PurchasePrice = packPrice,
                    Amount = packs * packPrice
                });
            }
            RefreshGrid();
            txtSearch.Clear();
            selectedProduct = null;
            txtSearch.Tag = null;
            txtQty.Text = "1";
            lstSuggest.Visible = false;
            txtSearch.Focus();
        }

        private void SetSelected(Product p)
        {
            selectedProduct = p;
            txtSearch.Tag = p;
            string packInfo = p.PackSize > 1m ? ("  [pack " + p.PackSize.ToString("0.####") + "]") : "";
            txtSearch.Text = p.ProductCode + " - " + p.ProductName + packInfo;
            lstSuggest.Visible = false;
        }

        private void ShowProductHistory()
        {
            Product p = selectedProduct;
            if (p == null && txtSearch.Tag is Product t) p = t;
            if (p == null)
            {
                string q = txtSearch.Text.Trim();
                if (q.Contains(" - ")) q = q.Split(new[] { " - " }, StringSplitOptions.None)[0].Trim();
                if (q.Length > 0)
                    p = productRepo.GetByBarcode(q) ?? productRepo.GetByCode(q);
            }
            if (p == null)
            {
                MessageBox.Show("Select or search a product first, then press F9.");
                return;
            }
            selectedProduct = p;
            using (var f = new ProductHistoryForm(p, false))
                f.ShowDialog(this);
        }

        private void ShowSuggestions()
        {
            string q = txtSearch.Text.Trim();
            if (q.Length < 2) { lstSuggest.Visible = false; return; }
            var list = productRepo.Search(q);
            lstSuggest.DataSource = null;
            lstSuggest.DataSource = list;
            lstSuggest.Visible = list.Count > 0;
            if (list.Count > 0) lstSuggest.SelectedIndex = 0;
            lstSuggest.BringToFront();
        }

        private void SelectSug()
        {
            if (lstSuggest.SelectedItem is Product p)
            {
                SetSelected(p);
                txtQty.Text = "1";
                txtQty.Focus();
                txtQty.SelectAll();
            }
        }

        private Panel BuildTotalsFooter()
        {
            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 78, BackColor = Color.FromArgb(232, 248, 238) };
            int fx = 16;
            AddFootLabel(foot, "Total", fx, 10); txtTotal = AddFootBox(foot, fx, 32, 110, "0.00");
            txtTotal.TextChanged += (s, e) => OnTotalChanged();
            txtTotal.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtDiscount.Focus(); txtDiscount.SelectAll(); } };
            fx += 126;
            AddFootLabel(foot, "Disc %", fx, 10); txtDiscount = AddFootBox(foot, fx, 32, 70, "0");
            txtDiscount.TextChanged += (s, e) => OnPctChanged();
            txtDiscount.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtDiscAmt.Focus(); txtDiscAmt.SelectAll(); } };
            fx += 86;
            AddFootLabel(foot, "Discount", fx, 10); txtDiscAmt = AddFootBox(foot, fx, 32, 110, "0.00");
            txtDiscAmt.TextChanged += (s, e) => OnAmtChanged();
            txtDiscAmt.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtNet.Focus(); txtNet.SelectAll(); } };
            fx += 126;
            AddFootLabel(foot, "Net", fx, 10); txtNet = AddFootBox(foot, fx, 32, 120, "0.00");
            txtNet.ForeColor = FormAccent.Purchase;
            txtNet.TextChanged += (s, e) => OnNetChanged();
            txtNet.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtPaid.Text = txtNet.Text; txtPaid.Focus(); txtPaid.SelectAll(); } };
            fx += 136;
            AddFootLabel(foot, "Paid", fx, 10); txtPaid = AddFootBox(foot, fx, 32, 110, "0.00");
            txtPaid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Save(); } };

            Button btnSave = new Button { Text = "SAVE (F12)", Size = new Size(130, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Button btnClose = new Button { Text = "CLOSE (F4)", Size = new Size(130, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            UiHelper.StyleAccentButton(btnSave, FormAccent.Purchase, FormAccent.PurchaseDark);
            UiHelper.StyleAccentButton(btnClose, FormAccent.PurchaseDark, FormAccent.Purchase);
            btnSave.Click += (s, e) => Save();
            btnClose.Click += (s, e) => this.Close();
            foot.Controls.Add(btnSave); foot.Controls.Add(btnClose);
            foot.Resize += (s, e) =>
            {
                btnClose.Location = new Point(foot.Width - 16 - btnClose.Width, 22);
                btnSave.Location = new Point(btnClose.Left - 10 - btnSave.Width, 22);
            };
            return foot;
        }

        private static void AddFootLabel(Control p, string t, int x, int y)
        {
            p.Controls.Add(new Label { Text = t, Font = UiHelper.SmallFont, Location = new Point(x, y), AutoSize = true });
        }

        private static TextBox AddFootBox(Control p, int x, int y, int w, string val)
        {
            var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 28), Text = val };
            UiHelper.StyleTextBox(t);
            p.Controls.Add(t);
            return t;
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = cart;
            foreach (var h in new[] { "PurchaseDetailID", "PurchaseID", "ProductID", "Discount" })
                if (dgv.Columns.Contains(h)) dgv.Columns[h].Visible = false;
            decimal baseTotal = 0;
            foreach (var i in cart) baseTotal += i.Amount;
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
            if (MessageBox.Show("Save this purchase?\nStock will increase by packs × pack size.", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            try
            {
                decimal total = ReadTotal();
                decimal discAmt = 0, paid = 0, net = 0;
                decimal.TryParse(txtDiscAmt.Text, out discAmt);
                decimal.TryParse(txtPaid.Text, out paid);
                decimal.TryParse(txtNet.Text, out net);
                discAmt = Math.Round(discAmt, 2);
                if (net <= 0) net = Math.Round(total - discAmt, 2);
                var header = new PurchaseHeader
                {
                    PurchaseDate = DateTime.Now,
                    SupplierID = selectedSupplier.SupplierID,
                    TotalAmount = total,
                    Discount = discAmt,
                    NetAmount = net,
                    PaidAmount = paid,
                    BalanceAmount = net - paid,
                    Details = cart
                };
                purchaseService.Save(header);
                MessageBox.Show("Purchase saved!\nInvoice: " + header.InvoiceNo, "Success");
                this.Tag = "NOSAVECONFIRM";
                cart.Clear();
                RefreshGrid();
                txtSearch.Clear();
                txtPaid.Text = "0.00";
                txtDiscount.Text = "0";
                selectedProduct = null;
                txtSearch.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
