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
    public class PurchaseForm : Form
    {
        private readonly PurchaseService purchaseService = new PurchaseService();
        private readonly ProductRepository productRepo = new ProductRepository();
        private readonly SupplierRepository supplierRepo = new SupplierRepository();
        private readonly List<PurchaseDetail> cart = new List<PurchaseDetail>();

        private Supplier selectedSupplier;
        private Product selectedProduct;

        private TextBox txtInvoice, txtSupplier, txtDescription, txtQty;
        private TextBox txtDiscount, txtDiscAmt, txtPaid, txtTotal, txtNet;
        private Label lblDate;
        private DataGridView dgv;
        private ListBox lstSupplier, lstProduct;

        private decimal cartBaseTotal;
        private bool calcBusy;

        private sealed class ProductSuggestRow
        {
            public Product Product { get; set; }
            public string Display { get; set; }
            public override string ToString() { return Display ?? ""; }
        }

        public PurchaseForm()
        {
            InitializeComponent();
            var list = supplierRepo.GetAllActive();
            if (list.Count > 0)
            {
                selectedSupplier = list[0];
                txtSupplier.Text = selectedSupplier.SupplierName;
            }
            txtDescription.Focus();
        }

        private void InitializeComponent()
        {
            Text = "Purchase";
            Size = new Size(1024, 700);
            MinimumSize = new Size(900, 560);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);
            Controls.Add(UiHelper.CreateFormBanner("PURCHASE", "Stock in - pack quantities", FormAccent.Purchase, FormAccent.PurchaseDark));

            var head = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = Color.White };
            head.Controls.Add(new Label { Text = "Invoice:", Font = UiHelper.SmallFont, Location = new Point(8, 14), AutoSize = true });
            txtInvoice = new TextBox { Location = new Point(60, 10), Size = new Size(100, 28), Text = "Auto", ReadOnly = true };
            UiHelper.StyleTextBox(txtInvoice);
            head.Controls.Add(txtInvoice);
            lblDate = new Label { Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm"), Font = UiHelper.NormalFont, ForeColor = Color.Gray, Location = new Point(180, 14), AutoSize = true };
            head.Controls.Add(lblDate);
            head.Controls.Add(new Label { Text = "Supplier:", Font = UiHelper.SmallFont, Location = new Point(380, 14), AutoSize = true });
            txtSupplier = new TextBox { Location = new Point(450, 10), Size = new Size(320, 28) };
            UiHelper.StyleTextBox(txtSupplier);
            txtSupplier.TextChanged += (s, e) => ShowSupplierSuggestions();
            txtSupplier.KeyDown += Supplier_KeyDown;
            head.Controls.Add(txtSupplier);
            Controls.Add(head);

            var entry = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White };
            entry.Controls.Add(new Label { Text = "Description (Product Name / Code)", Font = UiHelper.SmallFont, Location = new Point(8, 4), AutoSize = true });
            txtDescription = new TextBox { Location = new Point(8, 22), Size = new Size(520, 28) };
            UiHelper.StyleTextBox(txtDescription);
            txtDescription.TextChanged += (s, e) => ShowProductSuggestions();
            txtDescription.KeyDown += Description_KeyDown;
            entry.Controls.Add(txtDescription);
            entry.Controls.Add(new Label { Text = "Qty (packs)", Font = UiHelper.SmallFont, Location = new Point(540, 4), AutoSize = true });
            txtQty = new TextBox { Location = new Point(540, 22), Size = new Size(90, 28), Text = "1" };
            UiHelper.StyleTextBox(txtQty);
            txtQty.KeyDown += Qty_KeyDown;
            entry.Controls.Add(txtQty);
            Controls.Add(entry);

            lstSupplier = new ListBox { Visible = false, Font = UiHelper.NormalFont, IntegralHeight = false, Size = new Size(320, 140), DisplayMember = "SupplierName" };
            lstSupplier.Click += (s, e) => SelectSupplierSug();
            lstSupplier.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectSupplierSug(); }
                if (e.KeyCode == Keys.Escape) lstSupplier.Visible = false;
            };
            Controls.Add(lstSupplier);

            lstProduct = new ListBox { Visible = false, Font = UiHelper.NormalFont, IntegralHeight = false, Size = new Size(520, 160) };
            lstProduct.Click += (s, e) => SelectProductSug();
            lstProduct.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectProductSug(); }
                if (e.KeyCode == Keys.Escape) { lstProduct.Visible = false; txtDescription.Focus(); }
            };
            Controls.Add(lstProduct);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGrid(dgv);
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", DataPropertyName = "ProductName", HeaderText = "Description", FillWeight = 45 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", DataPropertyName = "ProductCode", HeaderText = "Code", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", DataPropertyName = "Quantity", HeaderText = "Qty (packs)", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", DataPropertyName = "PurchasePrice", HeaderText = "Purchase Price", DefaultCellStyle = { Format = "N2" }, FillWeight = 15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", DataPropertyName = "Amount", HeaderText = "Amount", DefaultCellStyle = { Format = "N2" }, FillWeight = 15 });
            Controls.Add(dgv);
            Controls.SetChildIndex(dgv, 0);

            var foot = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Color.White };
            int x = 12;
            foot.Controls.Add(new Label { Text = "Total", Font = UiHelper.SmallFont, Location = new Point(x, 10), AutoSize = true });
            txtTotal = MakeFootBox(foot, x, 32, 100, "0.00", true); x += 110;
            foot.Controls.Add(new Label { Text = "Disc %", Font = UiHelper.SmallFont, Location = new Point(x, 10), AutoSize = true });
            txtDiscount = MakeFootBox(foot, x, 32, 70, "0"); x += 80;
            foot.Controls.Add(new Label { Text = "Discount", Font = UiHelper.SmallFont, Location = new Point(x, 10), AutoSize = true });
            txtDiscAmt = MakeFootBox(foot, x, 32, 90, "0.00"); x += 100;
            foot.Controls.Add(new Label { Text = "Net", Font = UiHelper.SmallFont, Location = new Point(x, 10), AutoSize = true });
            txtNet = MakeFootBox(foot, x, 32, 100, "0.00", true); x += 110;
            foot.Controls.Add(new Label { Text = "Paid", Font = UiHelper.SmallFont, Location = new Point(x, 10), AutoSize = true });
            txtPaid = MakeFootBox(foot, x, 32, 100, "0.00");
            txtDiscount.TextChanged += (s, e) => RecalcFromPct();
            txtDiscAmt.TextChanged += (s, e) => RecalcFromAmt();
            txtDiscount.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtDiscAmt.Focus(); txtDiscAmt.SelectAll(); } };
            txtDiscAmt.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtPaid.Text = txtNet.Text; txtPaid.Focus(); txtPaid.SelectAll(); } };
            txtPaid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Save(); } };

            var btnSave = new Button { Text = "SAVE (F12)", Size = new Size(130, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var btnClose = new Button { Text = "CLOSE (F4)", Size = new Size(120, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            UiHelper.StyleButton(btnSave);
            UiHelper.StyleButton(btnClose);
            btnSave.Click += (s, e) => Save();
            btnClose.Click += (s, e) => Close();
            foot.Controls.Add(btnSave);
            foot.Controls.Add(btnClose);
            foot.Resize += (s, e) =>
            {
                btnClose.Location = new Point(foot.Width - 16 - btnClose.Width, 28);
                btnSave.Location = new Point(btnClose.Left - 10 - btnSave.Width, 28);
            };
            Controls.Add(foot);

            KeyDown += Form_KeyDown;
            Shown += (s, e) => { lstSupplier.BringToFront(); lstProduct.BringToFront(); txtDescription.Focus(); };
        }

        private static TextBox MakeFootBox(Control p, int x, int y, int w, string val, bool readOnly = false)
        {
            var t = new TextBox { Location = new Point(x, y), Size = new Size(w, 28), Text = val, ReadOnly = readOnly };
            UiHelper.StyleTextBox(t);
            p.Controls.Add(t);
            return t;
        }

        private void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12) { e.Handled = true; txtDiscount.Focus(); txtDiscount.SelectAll(); }
            if (e.KeyCode == Keys.F8) { e.Handled = true; RemoveSelectedLine(); }
            if (e.KeyCode == Keys.F9) { e.Handled = true; ShowProductHistory(); }
        }

        private void ShowSupplierSuggestions()
        {
            string q = (txtSupplier.Text ?? "").Trim();
            var list = supplierRepo.Search(q);
            lstSupplier.DataSource = null;
            lstSupplier.DisplayMember = "SupplierName";
            lstSupplier.DataSource = list;
            lstSupplier.Visible = list.Count > 0;
            if (list.Count > 0) lstSupplier.SelectedIndex = 0;
            PositionList(lstSupplier, txtSupplier);
        }

        private void Supplier_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && lstSupplier.Visible) { lstSupplier.Focus(); e.Handled = true; return; }
            if (e.KeyCode == Keys.Escape) { lstSupplier.Visible = false; return; }
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (lstSupplier.Visible && lstSupplier.SelectedItem is Supplier) SelectSupplierSug();
            else
            {
                var list = supplierRepo.Search(txtSupplier.Text);
                if (list.Count > 0) { selectedSupplier = list[0]; txtSupplier.Text = selectedSupplier.SupplierName; }
                lstSupplier.Visible = false;
                txtDescription.Focus();
            }
        }

        private void SelectSupplierSug()
        {
            if (lstSupplier.SelectedItem is Supplier s)
            {
                selectedSupplier = s;
                txtSupplier.Text = s.SupplierName;
                lstSupplier.Visible = false;
                txtDescription.Focus();
            }
        }

        private void ShowProductSuggestions()
        {
            string q = (txtDescription.Text ?? "").Trim();
            if (q.Length < 1) { lstProduct.Visible = false; return; }
            List<Product> products;
            try { products = productRepo.Search(q) ?? new List<Product>(); }
            catch { products = new List<Product>(); }
            var rows = new List<ProductSuggestRow>();
            foreach (Product p in products)
            {
                if (p == null) continue;
                rows.Add(new ProductSuggestRow
                {
                    Product = p,
                    Display = (p.ProductCode ?? "") + " - " + (p.ProductName ?? "") + "  |  Cost: " + p.PurchasePrice.ToString("0.00")
                });
            }
            lstProduct.BeginUpdate();
            try
            {
                lstProduct.DataSource = null;
                lstProduct.DisplayMember = "Display";
                lstProduct.ValueMember = "Product";
                lstProduct.DataSource = rows;
            }
            finally { lstProduct.EndUpdate(); }
            lstProduct.Visible = rows.Count > 0;
            if (rows.Count > 0) lstProduct.SelectedIndex = 0;
            PositionList(lstProduct, txtDescription);
        }

        private void Description_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && lstProduct.Visible) { lstProduct.Focus(); e.Handled = true; return; }
            if (e.KeyCode == Keys.Escape) { lstProduct.Visible = false; return; }
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            if (lstProduct.Visible && lstProduct.SelectedItem != null) { SelectProductSug(); return; }
            string q = (txtDescription.Text ?? "").Trim();
            Product p = null;
            try { p = productRepo.GetByCode(q) ?? productRepo.GetByBarcode(q); } catch { }
            if (p == null)
            {
                try { var list = productRepo.Search(q); if (list != null && list.Count > 0) p = list[0]; } catch { }
            }
            if (p == null) { DialogHelpers.Warn(this, "Product not found."); return; }
            SetSelectedProduct(p);
            txtQty.Focus();
            txtQty.SelectAll();
        }

        private void SelectProductSug()
        {
            var row = lstProduct.SelectedItem as ProductSuggestRow;
            if (row == null || row.Product == null) return;
            SetSelectedProduct(row.Product);
            lstProduct.Visible = false;
            txtQty.Focus();
            txtQty.SelectAll();
        }

        private void SetSelectedProduct(Product p)
        {
            selectedProduct = p;
            txtDescription.Text = (p.ProductCode ?? "") + " - " + (p.ProductName ?? "");
            txtQty.Text = "1";
        }

        private void Qty_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            AddLine();
        }

        private void AddLine()
        {
            if (selectedProduct == null)
            {
                Description_KeyDown(txtDescription, new KeyEventArgs(Keys.Enter));
                if (selectedProduct == null) return;
            }
            Product p = selectedProduct;
            int packs = 1;
            int.TryParse(txtQty.Text, out packs);
            if (packs < 1) packs = 1;
            decimal packPrice = PackMath.PackPurchasePrice(p);
            var ex = cart.Find(line => line.ProductID == p.ProductID);
            if (ex != null)
            {
                ex.Quantity += packs;
                ex.Amount = Math.Round(ex.Quantity * ex.PurchasePrice, 2);
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
                    Amount = Math.Round(packs * packPrice, 2)
                });
            }
            selectedProduct = null;
            txtDescription.Clear();
            txtQty.Text = "1";
            lstProduct.Visible = false;
            RefreshGrid();
            txtDescription.Focus();
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = new List<PurchaseDetail>(cart);
            cartBaseTotal = 0;
            foreach (var i in cart) cartBaseTotal += i.Amount;
            calcBusy = true;
            txtTotal.Text = cartBaseTotal.ToString("0.00");
            calcBusy = false;
            RecalcFromPct();
        }

        private void RecalcFromPct()
        {
            if (calcBusy) return;
            calcBusy = true;
            decimal pct = 0;
            decimal.TryParse(txtDiscount.Text, out pct);
            if (pct < 0) pct = 0;
            if (pct > 100) pct = 100;
            decimal disc = Math.Round(cartBaseTotal * pct / 100m, 2);
            txtDiscAmt.Text = disc.ToString("0.00");
            decimal net = Math.Round(cartBaseTotal - disc, 2);
            txtNet.Text = net.ToString("0.00");
            if (string.IsNullOrWhiteSpace(txtPaid.Text) || txtPaid.Text == "0" || txtPaid.Text == "0.00")
                txtPaid.Text = net.ToString("0.00");
            calcBusy = false;
        }

        private void RecalcFromAmt()
        {
            if (calcBusy) return;
            calcBusy = true;
            decimal disc = 0;
            decimal.TryParse(txtDiscAmt.Text, out disc);
            if (disc < 0) disc = 0;
            if (disc > cartBaseTotal) disc = cartBaseTotal;
            decimal pct = cartBaseTotal > 0 ? Math.Round(disc * 100m / cartBaseTotal, 2) : 0;
            txtDiscount.Text = pct.ToString("0.##");
            txtNet.Text = Math.Round(cartBaseTotal - disc, 2).ToString("0.00");
            calcBusy = false;
        }

        private void RemoveSelectedLine()
        {
            if (dgv.CurrentRow == null || dgv.CurrentRow.Index < 0 || dgv.CurrentRow.Index >= cart.Count) return;
            cart.RemoveAt(dgv.CurrentRow.Index);
            RefreshGrid();
        }

        private void ShowProductHistory()
        {
            Product p = selectedProduct;
            if (p == null && dgv.CurrentRow != null && dgv.CurrentRow.Index >= 0 && dgv.CurrentRow.Index < cart.Count)
                p = productRepo.GetById(cart[dgv.CurrentRow.Index].ProductID);
            if (p == null) { DialogHelpers.Warn(this, "Select or search a product first, then press F9."); return; }
            using (var f = new ProductHistoryForm(p, false))
                f.ShowDialog(this);
        }

        private void PositionList(ListBox lst, Control anchor)
        {
            try
            {
                Point screen = anchor.PointToScreen(new Point(0, anchor.Height));
                Point client = PointToClient(screen);
                lst.Location = new Point(client.X, client.Y + 2);
                lst.Width = Math.Max(lst.Width, anchor.Width);
                lst.BringToFront();
            }
            catch { }
        }

        private void Save()
        {
            if (cart.Count == 0) { DialogHelpers.Warn(this, "Add at least one product."); return; }
            if (selectedSupplier == null) { DialogHelpers.Error(this, "Select a supplier."); txtSupplier.Focus(); return; }

            decimal total = cartBaseTotal;
            decimal discAmt = 0, paid = 0, net = 0;
            decimal.TryParse(txtDiscAmt.Text, out discAmt);
            decimal.TryParse(txtPaid.Text, out paid);
            decimal.TryParse(txtNet.Text, out net);
            discAmt = Math.Round(discAmt, 2);
            if (net <= 0) net = Math.Round(total - discAmt, 2);

            if (!DialogHelpers.Confirm(this, "Save this purchase?")) return;
            try
            {
                var purchase = new PurchaseHeader
                {
                    PurchaseDate = DateTime.Now,
                    SupplierID = selectedSupplier.SupplierID,
                    SupplierName = selectedSupplier.SupplierName,
                    TotalAmount = total,
                    Discount = discAmt,
                    NetAmount = net,
                    PaidAmount = paid,
                    BalanceAmount = net - paid,
                    Details = new List<PurchaseDetail>(cart)
                };
                purchaseService.Save(purchase);
                DialogHelpers.Info(this, "Purchase saved!\nInvoice: " + purchase.InvoiceNo);
                Tag = "NOSAVECONFIRM";
                if (MainForm.Instance != null)
                    MainForm.Instance.OpenChild(new PurchaseForm(), "PURCHASE");
                Close();
            }
            catch (Exception ex)
            {
                AppLog.Error("Purchase save UI error", ex);
                DialogHelpers.Error(this, "Error: " + ex.Message);
            }
        }
    }
}
