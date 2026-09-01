using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    public class SaleReturnForm : Form
    {
        private readonly SaleReturnService _svc = new SaleReturnService();
        private readonly ProductRepository _products = new ProductRepository();
        private readonly List<SaleReturnDetail> cart = new List<SaleReturnDetail>();
        private List<SaleInvoiceLine> invoiceLines = new List<SaleInvoiceLine>();

        private TextBox txtInvoice, txtSearch, txtQty, txtReason, txtDiscount, txtRefund, txtTotal, txtNet;
        private Label lblCustomer, lblInfo;
        private DataGridView dgv;
        private Product selectedProduct;
        private ListBox lstSuggest;
        private int originalSaleId;
        private int customerId;
        private string customerName;

        public SaleReturnForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Sale Return";
            Size = new Size(980, 640);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);
            UiHelper.AttachEnterNavigation(this);

            Controls.Add(UiHelper.CreateFormBanner(
                "SALE RETURN",
                "Optional invoice · Type product to search · Reason required · Stock increases · F12 save",
                FormAccent.SaleReturn, FormAccent.SaleReturnDark));

            var invBar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.White, Padding = new Padding(10, 8, 10, 6) };
            invBar.Controls.Add(new Label { Text = "Original Inv (optional)", Location = new Point(8, 12), AutoSize = true, Font = UiHelper.SmallFont });
            txtInvoice = new TextBox { Location = new Point(150, 8), Size = new Size(130, 28) };
            UiHelper.StyleTextBox(txtInvoice);
            invBar.Controls.Add(txtInvoice);
            var btnLoad = new Button { Text = "LOAD (F5)", Location = new Point(290, 6), Size = new Size(100, 32) };
            UiHelper.StyleAccentButton(btnLoad, FormAccent.SaleReturn, FormAccent.SaleReturnDark);
            btnLoad.Click += (s, e) => LoadInvoice();
            invBar.Controls.Add(btnLoad);
            lblCustomer = new Label { Text = "Customer: —", Location = new Point(410, 12), AutoSize = true, Font = UiHelper.NormalFont, ForeColor = FormAccent.SaleReturnDark };
            invBar.Controls.Add(lblCustomer);
            lblInfo = new Label { Text = "Type in search to load products", Location = new Point(620, 12), AutoSize = true, Font = UiHelper.SmallFont, ForeColor = Color.Gray };
            invBar.Controls.Add(lblInfo);
            Controls.Add(invBar);

            var top = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.FromArgb(255, 248, 240), Padding = new Padding(10, 8, 10, 6) };
            top.Controls.Add(new Label { Text = "Product code / name", Location = new Point(8, 6), AutoSize = true, Font = UiHelper.SmallFont });
            txtSearch = new TextBox { Location = new Point(8, 26), Size = new Size(280, 28) };
            UiHelper.StyleTextBox(txtSearch);
            txtSearch.TextChanged += (s, e) => ShowSuggestions();
            txtSearch.KeyDown += Search_KeyDown;
            top.Controls.Add(txtSearch);

            top.Controls.Add(new Label { Text = "Qty", Location = new Point(300, 6), AutoSize = true, Font = UiHelper.SmallFont });
            txtQty = new TextBox { Location = new Point(300, 26), Size = new Size(70, 28), Text = "1" };
            UiHelper.StyleTextBox(txtQty);
            txtQty.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddLine(); } };
            top.Controls.Add(txtQty);

            var btnAdd = new Button { Text = "ADD", Location = new Point(385, 24), Size = new Size(80, 32) };
            UiHelper.StyleAccentButton(btnAdd, FormAccent.SaleReturn, FormAccent.SaleReturnDark);
            btnAdd.Click += (s, e) => AddLine();
            top.Controls.Add(btnAdd);
            Controls.Add(top);

            lstSuggest = new ListBox
            {
                Location = new Point(18, 200),
                Size = new Size(420, 160),
                Visible = false,
                Font = UiHelper.NormalFont,
                IntegralHeight = false
            };
            lstSuggest.Click += (s, e) => SelectSuggestion();
            lstSuggest.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; SelectSuggestion(); }
            };
            Controls.Add(lstSuggest);
            lstSuggest.BringToFront();

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.SaleReturn);
            dgv.AutoGenerateColumns = false;
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", DataPropertyName = "ProductName", HeaderText = "Description", FillWeight = 40 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", DataPropertyName = "ProductCode", HeaderText = "Code", FillWeight = 12 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", DataPropertyName = "Quantity", HeaderText = "Qty", FillWeight = 10 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "SalePrice", DataPropertyName = "SalePrice", HeaderText = "Rate", DefaultCellStyle = { Format = "N2" }, FillWeight = 14 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Amount", DataPropertyName = "Amount", HeaderText = "Net Amount", DefaultCellStyle = { Format = "N2" }, FillWeight = 14 });
            dgv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete && dgv.CurrentRow != null)
                {
                    int i = dgv.CurrentRow.Index;
                    if (i >= 0 && i < cart.Count) { cart.RemoveAt(i); RefreshGrid(); }
                }
            };
            Controls.Add(dgv);

            var foot = new Panel { Dock = DockStyle.Bottom, Height = 110, BackColor = Color.FromArgb(255, 243, 224) };
            foot.Controls.Add(new Label { Text = "Return reason (required)", Location = new Point(12, 10), AutoSize = true, Font = UiHelper.SmallFont });
            txtReason = new TextBox { Location = new Point(12, 30), Size = new Size(320, 28) };
            UiHelper.StyleTextBox(txtReason);
            foot.Controls.Add(txtReason);

            foot.Controls.Add(new Label { Text = "Discount", Location = new Point(350, 10), AutoSize = true, Font = UiHelper.SmallFont });
            txtDiscount = new TextBox { Location = new Point(350, 30), Size = new Size(80, 28), Text = "0" };
            UiHelper.StyleTextBox(txtDiscount);
            txtDiscount.TextChanged += (s, e) => Recalc();
            foot.Controls.Add(txtDiscount);

            foot.Controls.Add(new Label { Text = "Refund", Location = new Point(450, 10), AutoSize = true, Font = UiHelper.SmallFont });
            txtRefund = new TextBox { Location = new Point(450, 30), Size = new Size(90, 28), Text = "0.00" };
            UiHelper.StyleTextBox(txtRefund);
            foot.Controls.Add(txtRefund);

            txtTotal = new TextBox { Location = new Point(560, 30), Size = new Size(90, 28), ReadOnly = true, Text = "0.00" };
            UiHelper.StyleTextBox(txtTotal);
            foot.Controls.Add(new Label { Text = "Total", Location = new Point(560, 10), AutoSize = true, Font = UiHelper.SmallFont });
            foot.Controls.Add(txtTotal);

            txtNet = new TextBox { Location = new Point(670, 30), Size = new Size(90, 28), ReadOnly = true, Text = "0.00" };
            UiHelper.StyleTextBox(txtNet);
            foot.Controls.Add(new Label { Text = "Net", Location = new Point(670, 10), AutoSize = true, Font = UiHelper.SmallFont });
            foot.Controls.Add(txtNet);

            var btnSave = new Button { Text = "SAVE RETURN (F12)", Location = new Point(780, 24), Size = new Size(160, 36) };
            UiHelper.StyleAccentButton(btnSave, FormAccent.SaleReturn, FormAccent.SaleReturnDark);
            btnSave.Click += (s, e) => Save();
            foot.Controls.Add(btnSave);

            var btnClose = new Button { Text = "CLOSE (F4)", Location = new Point(780, 66), Size = new Size(160, 28) };
            UiHelper.StyleButton(btnClose);
            btnClose.Click += (s, e) => Close();
            foot.Controls.Add(btnClose);
            Controls.Add(foot);
            Controls.SetChildIndex(dgv, 0);
            Controls.SetChildIndex(foot, 0);

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F5) { LoadInvoice(); e.Handled = true; }
                if (e.KeyCode == Keys.F12) { Save(); e.Handled = true; }
            };
        }

        private void LoadInvoice()
        {
            try
            {
                invoiceLines = _svc.LoadInvoice(txtInvoice.Text.Trim());
                if (invoiceLines.Count == 0) return;
                originalSaleId = invoiceLines[0].SaleID;
                customerId = invoiceLines[0].CustomerID;
                customerName = invoiceLines[0].CustomerName;
                lblCustomer.Text = "Customer: " + customerName + "  ·  Sale# " + invoiceLines[0].InvoiceNo;
                lblInfo.Text = invoiceLines.Count + " line(s) — type product name/code";
                DialogHelpers.Info(this, "Invoice loaded. Type product to search.");
                txtSearch.Focus();
            }
            catch (Exception ex)
            {
                DialogHelpers.Error(this, ex.Message);
            }
        }

        private void ShowSuggestions()
        {
            string q = (txtSearch.Text ?? "").Trim();
            if (lstSuggest == null) return;
            if (q.Length < 1)
            {
                lstSuggest.Visible = false;
                return;
            }

            var list = new List<Product>();
            if (invoiceLines != null && invoiceLines.Count > 0)
            {
                foreach (var line in invoiceLines)
                {
                    if ((line.ProductCode != null && line.ProductCode.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (line.ProductName != null && line.ProductName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        list.Add(new Product
                        {
                            ProductID = line.ProductID,
                            ProductCode = line.ProductCode,
                            ProductName = line.ProductName,
                            SalePrice = line.SalePrice
                        });
                    }
                }
            }
            if (list.Count == 0)
            {
                var found = _products.Search(q);
                if (found != null) list.AddRange(found);
            }

            lstSuggest.DataSource = null;
            lstSuggest.DisplayMember = "ProductName";
            lstSuggest.DataSource = list;
            lstSuggest.Visible = list.Count > 0;
            if (list.Count > 0) lstSuggest.SelectedIndex = 0;
            try
            {
                Point screen = txtSearch.PointToScreen(new Point(0, txtSearch.Height));
                Point client = PointToClient(screen);
                lstSuggest.Location = new Point(client.X, client.Y + 2);
                lstSuggest.Width = Math.Max(320, txtSearch.Width + 100);
            }
            catch { }
            lstSuggest.BringToFront();
        }

        private void SelectSuggestion()
        {
            if (lstSuggest == null || !lstSuggest.Visible) return;
            if (lstSuggest.SelectedItem is Product p)
            {
                selectedProduct = p;
                txtSearch.Text = p.ProductCode + " - " + p.ProductName;
                lstSuggest.Visible = false;
                txtQty.Focus();
                txtQty.SelectAll();
            }
        }

        private void Search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && lstSuggest != null && lstSuggest.Visible)
            {
                lstSuggest.Focus();
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Escape && lstSuggest != null)
            {
                lstSuggest.Visible = false;
                return;
            }
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;

            if (lstSuggest != null && lstSuggest.Visible && lstSuggest.SelectedItem is Product)
            {
                SelectSuggestion();
                return;
            }

            string q = (txtSearch.Text ?? "").Trim();
            if (q.Length == 0) return;

            if (invoiceLines != null && invoiceLines.Count > 0)
            {
                var line = invoiceLines.FirstOrDefault(x =>
                    string.Equals(x.ProductCode, q, StringComparison.OrdinalIgnoreCase) ||
                    (x.ProductName != null && x.ProductName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
                if (line != null)
                {
                    selectedProduct = _products.GetById(line.ProductID) ?? new Product
                    {
                        ProductID = line.ProductID,
                        ProductCode = line.ProductCode,
                        ProductName = line.ProductName,
                        SalePrice = line.SalePrice
                    };
                    txtSearch.Text = line.ProductCode + " - " + line.ProductName;
                    if (lstSuggest != null) lstSuggest.Visible = false;
                    txtQty.Focus();
                    txtQty.SelectAll();
                    return;
                }
            }

            var p = _products.GetByCode(q) ?? _products.GetByBarcode(q);
            if (p == null)
            {
                var list = _products.Search(q);
                if (list != null && list.Count > 0) p = list[0];
            }
            if (p == null)
            {
                DialogHelpers.Warn(this, "Product not found.");
                return;
            }
            selectedProduct = p;
            txtSearch.Text = p.ProductCode + " - " + p.ProductName;
            if (lstSuggest != null) lstSuggest.Visible = false;
            txtQty.Focus();
            txtQty.SelectAll();
        }

        private void AddLine()
        {
            if (selectedProduct == null)
            {
                Search_KeyDown(txtSearch, new KeyEventArgs(Keys.Enter));
                if (selectedProduct == null) return;
            }
            int qty = 1;
            int.TryParse(txtQty.Text, out qty);
            if (qty < 1) qty = 1;

            int? origDetail = null;
            int sold = 0, already = 0;
            decimal rate = selectedProduct.SalePrice;

            if (invoiceLines != null)
            {
                var line = invoiceLines.FirstOrDefault(x => x.ProductID == selectedProduct.ProductID);
                if (line != null)
                {
                    origDetail = line.SaleDetailID;
                    sold = line.SoldQty;
                    already = line.AlreadyReturned;
                    rate = line.SalePrice;
                    int returnable = Math.Max(0, sold - already);
                    int inCart = cart.Where(c => c.ProductID == selectedProduct.ProductID).Sum(c => c.Quantity);
                    if (qty + inCart > returnable)
                    {
                        DialogHelpers.Warn(this, "Returnable qty is " + returnable + " (sold " + sold + " − returned " + already + ").");
                        return;
                    }
                }
            }

            var existing = cart.FirstOrDefault(c => c.ProductID == selectedProduct.ProductID && c.OriginalSaleDetailID == origDetail);
            if (existing != null)
            {
                existing.Quantity += qty;
                existing.Amount = Math.Round(existing.Quantity * existing.SalePrice, 2);
            }
            else
            {
                cart.Add(new SaleReturnDetail
                {
                    OriginalSaleDetailID = origDetail,
                    ProductID = selectedProduct.ProductID,
                    ProductCode = selectedProduct.ProductCode,
                    ProductName = selectedProduct.ProductName,
                    Quantity = qty,
                    SalePrice = rate,
                    Amount = Math.Round(qty * rate, 2),
                    SoldQty = sold,
                    AlreadyReturned = already
                });
            }

            selectedProduct = null;
            txtSearch.Clear();
            txtQty.Text = "1";
            if (lstSuggest != null) lstSuggest.Visible = false;
            RefreshGrid();
            txtSearch.Focus();
        }

        private void RefreshGrid()
        {
            dgv.DataSource = null;
            dgv.DataSource = cart.ToList();
            Recalc();
        }

        private void Recalc()
        {
            decimal gross = cart.Sum(c => c.Amount);
            decimal disc = 0;
            decimal.TryParse(txtDiscount.Text, out disc);
            if (disc < 0) disc = 0;
            decimal net = Math.Round(gross - disc, 2);
            txtTotal.Text = gross.ToString("0.00");
            txtNet.Text = net.ToString("0.00");
            if (string.IsNullOrWhiteSpace(txtRefund.Text) || txtRefund.Text == "0.00" || txtRefund.Text == "0")
                txtRefund.Text = net.ToString("0.00");
        }

        private void Save()
        {
            if (cart.Count == 0)
            {
                DialogHelpers.Warn(this, "Add at least one product to return.");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                DialogHelpers.Warn(this, "Return reason is required.");
                txtReason.Focus();
                return;
            }
            if (!DialogHelpers.Confirm(this, "Save sale return? Stock will increase."))
                return;

            decimal disc = 0, refund = 0;
            decimal.TryParse(txtDiscount.Text, out disc);
            decimal.TryParse(txtRefund.Text, out refund);

            var ret = new SaleReturnHeader
            {
                OriginalSaleID = originalSaleId,
                OriginalInvoiceNo = (txtInvoice.Text ?? "").Trim(),
                CustomerID = customerId > 0 ? customerId : 1,
                CustomerName = customerName,
                Discount = disc,
                RefundAmount = refund,
                Remarks = txtReason.Text.Trim(),
                ReturnDate = DateTime.Now,
                Details = cart.ToList()
            };

            try
            {
                _svc.Save(ret);
                DialogHelpers.Info(this, "Sale return saved.\nReturn No: " + ret.ReturnNo);
                cart.Clear();
                invoiceLines.Clear();
                originalSaleId = 0;
                customerId = 0;
                RefreshGrid();
                txtReason.Clear();
                txtInvoice.Clear();
                lblCustomer.Text = "Customer: —";
                txtSearch.Focus();
            }
            catch (Exception ex)
            {
                DialogHelpers.Error(this, ex.Message);
            }
        }
    }
}
