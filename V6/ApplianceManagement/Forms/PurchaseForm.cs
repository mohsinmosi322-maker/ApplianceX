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

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGrid(dgv);
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.ReadOnly = true;
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", DataPropertyName = "ProductName", HeaderText = "Description", FillWeight = 42 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", DataPropertyName = "ProductCode", HeaderText = "Code", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", DataPropertyName = "Quantity", HeaderText = "Qty (packs)", FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchasePrice", DataPropertyName = "PurchasePrice", HeaderText = "Purchase Price", FillWeight = 16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", DataPropertyName = "Amount", HeaderText = "Amount", FillWeight = 16 });
            Controls.Add(dgv);

            Controls.Add(BuildFooter());

            var entry = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.White,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(12, 4, 12, 6)
            };
            entry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            entry.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110f));
            entry.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            entry.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            entry.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));

            entry.Controls.Add(new Label { Text = "Description (Product Name / Code)", Font = UiHelper.SmallFont, AutoSize = true }, 0, 0);
            entry.Controls.Add(new Label { Text = "Qty (packs)", Font = UiHelper.SmallFont, AutoSize = true }, 1, 0);

            txtDescription = new TextBox { Dock = DockStyle.Fill };
            UiHelper.StyleTextBox(txtDescription);
            txtDescription.TextChanged += (s, e) => ShowProductSuggestions();
            txtDescription.KeyDown += Description_KeyDown;
            entry.Controls.Add(txtDescription, 0, 1);

            txtQty = new TextBox { Dock = DockStyle.Fill, Text = "1", TextAlign = HorizontalAlignment.Center };
            UiHelper.StyleTextBox(txtQty);
            txtQty.KeyDown += Qty_KeyDown;
            entry.Controls.Add(txtQty, 1, 1);

            entry.Controls.Add(new Label
            {
                Text = "Enter = add   F8 remove   F9 history   F12 discount",
                Font = UiHelper.SmallFont,
                ForeColor = Color.Gray,
                AutoSize = true
            }, 2, 1);
            Controls.Add(entry);

            var head = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 48,
                BackColor = Color.White,
                ColumnCount = 6,
                RowCount = 1,
                Padding = new Padding(12, 8, 12, 6)
            };
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 8f));

            head.Controls.Add(new Label { Text = "Invoice", Font = UiHelper.SmallFont, AutoSize = true }, 0, 0);
            txtInvoice = new TextBox { Dock = DockStyle.Fill, Text = "Auto", ReadOnly = true };
            UiHelper.StyleTextBox(txtInvoice);
            head.Controls.Add(txtInvoice, 1, 0);

            lblDate = new Label
            {
                Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm"),
                Font = UiHelper.NormalFont,
                ForeColor = Color.Gray,
                AutoSize = true
            };
            head.Controls.Add(lblDate, 2, 0);

            head.Controls.Add(new Label { Text = "Supplier", Font = UiHelper.SmallFont, AutoSize = true }, 3, 0);
            txtSupplier = new TextBox { Dock = DockStyle.Fill };
            UiHelper.StyleTextBox(txtSupplier);
            txtSupplier.TextChanged += (s, e) => ShowSupplierSuggestions();
            txtSupplier.KeyDown += Supplier_KeyDown;
            head.Controls.Add(txtSupplier, 4, 0);
            Controls.Add(head);

            Controls.Add(UiHelper.CreateFormBanner("PURCHASE", "Stock in - pack quantities", FormAccent.Purchase, FormAccent.PurchaseDark));

            lstSupplier = new ListBox
            {
                Visible = false,
                Font = UiHelper.NormalFont,
                IntegralHeight = false,
                Size = new Size(360, 140),
                DisplayMember = "SupplierName"
            };
            lstSupplier.Click += (s, e) => SelectSupplierSug();
            lstSupplier.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectSupplierSug(); }
                if (e.KeyCode == Keys.Escape) lstSupplier.Visible = false;
            };
            Controls.Add(lstSupplier);

            lstProduct = new ListBox
            {
                Visible = false,
                Font = UiHelper.NormalFont,
                IntegralHeight = false,
                Size = new Size(520, 160)
            };
            lstProduct.Click += (s, e) => SelectProductSug();
            lstProduct.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectProductSug(); }
                if (e.KeyCode == Keys.Escape) { lstProduct.Visible = false; txtDescription.Focus(); }
            };
            Controls.Add(lstProduct);

            KeyDown += Form_KeyDown;
            Shown += (s, e) =>
            {
                lstSupplier.BringToFront();
                lstProduct.BringToFront();
                txtDescription.Focus();
            };
        }

        private Panel BuildFooter()
        {
            var foot = new Panel { Dock = DockStyle.Bottom, Height = 88, BackColor = Color.White };
            var totals = new TableLayoutPanel
            {
                Dock = DockStyle.Left,
                Width = 620,
                ColumnCount = 5,
                RowCount = 2,
                Padding = new Padding(12, 6, 8, 6)
            };
            for (int i = 0; i < 5; i++)
                totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
            totals.RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
            totals.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));

            string[] labels = { "Total", "Disc %", "Discount", "Net", "Paid" };
            for (int i = 0; i < 5; i++)
                totals.Controls.Add(new Label { Text = labels[i], Font = UiHelper.SmallFont, AutoSize = true }, i, 0);

            txtTotal = FootBox("0.00", true);
            txtDiscount = FootBox("0", false);
            txtDiscAmt = FootBox("0.00", false);
            txtNet = FootBox("0.00", true);
            txtPaid = FootBox("0.00", false);
            totals.Controls.Add(txtTotal, 0, 1);
            totals.Controls.Add(txtDiscount, 1, 1);
            totals.Controls.Add(txtDiscAmt, 2, 1);
            totals.Controls.Add(txtNet, 3, 1);
            totals.Controls.Add(txtPaid, 4, 1);

            txtDiscount.TextChanged += (s, e) => RecalcFromPct();
            txtDiscAmt.TextChanged += (s, e) => RecalcFromAmt();
            txtDiscount.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtDiscAmt.Focus(); txtDiscAmt.SelectAll(); } };
            txtDiscAmt.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; txtPaid.Text = txtNet.Text; txtPaid.Focus(); txtPaid.SelectAll(); } };
            txtPaid.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Save(); } };

            foot.Controls.Add(totals);

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
            return foot;
        }

        private static TextBox FootBox(string val, bool readOnly)
        {
            var t = new TextBox { Dock = DockStyle.Fill, Text = val, ReadOnly = readOnly, TextAlign = HorizontalAlignment.Right };
            UiHelper.StyleTextBox(t);
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
            if (lstSupplier.Visible && lstSupplier.SelectedItem is Supplier)
                SelectSupplierSug();
            else
            {
                var list = supplierRepo.Search(txtSupplier.Text);
                if (list.Count > 0)
                {
                    selectedSupplier = list[0];
                    txtSupplier.Text = selectedSupplier.SupplierName;
                }
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
            decimal packPrice = p.PurchasePrice;
            if (packPrice < 0) packPrice = 0;
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
            if (p == null)
            {
                DialogHelpers.Warn(this, "Select or search a product first, then press F9.");
                return;
            }
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
            if (selectedSupplier == null)
            {
                DialogHelpers.Error(this, "Select a supplier.");
                txtSupplier.Focus();
                return;
            }

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
