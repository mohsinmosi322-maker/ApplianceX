using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    /// <summary>
    /// Invoice-linked sale return: load original INV → choose return qty ≤ returnable → reason → save.
    /// </summary>
    public partial class SaleReturnForm : Form
    {
        private readonly SaleReturnService _service = new SaleReturnService();
        private List<SaleInvoiceLine> _invoiceLines = new List<SaleInvoiceLine>();
        private int _saleId;
        private int _customerId;
        private string _customerName = "";
        private string _invoiceNo = "";

        private TextBox txtInvoice, txtReason, txtRefund;
        private Label lblCustomer, lblSaleDate, lblReturnNo;
        private DataGridView dgv;
        private Button btnLoad, btnSave;

        public SaleReturnForm()
        {
            InitializeComponent();
            txtInvoice.Focus();
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
                if (e.KeyCode == Keys.F12) { Save(); e.Handled = true; }
                if (e.KeyCode == Keys.F5) { LoadInvoice(); e.Handled = true; }
            };

            this.Controls.Add(UiHelper.CreateFormBanner(
                "SALE RETURN",
                "Load original invoice  ·  Return qty ≤ sold − already returned  ·  Reason required  ·  F5 load  F12 save",
                FormAccent.SaleReturn, FormAccent.SaleReturnDark));

            Panel top = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.White, Padding = new Padding(12) };
            top.Controls.Add(new Label { Text = "Original Invoice", Font = UiHelper.SmallFont, Location = new Point(16, 12), AutoSize = true });
            txtInvoice = new TextBox { Location = new Point(16, 32), Size = new Size(200, 28) };
            UiHelper.StyleTextBox(txtInvoice);
            txtInvoice.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadInvoice(); } };
            top.Controls.Add(txtInvoice);

            btnLoad = new Button { Text = "LOAD (F5)", Location = new Point(230, 30), Size = new Size(110, 32) };
            UiHelper.StyleAccentButton(btnLoad, FormAccent.SaleReturn, FormAccent.SaleReturnDark);
            btnLoad.Click += (s, e) => LoadInvoice();
            top.Controls.Add(btnLoad);

            lblReturnNo = new Label { Text = "Return No: (auto)", Font = UiHelper.HeaderFont, ForeColor = FormAccent.SaleReturnDark, Location = new Point(360, 12), AutoSize = true };
            lblCustomer = new Label { Text = "Customer: —", Font = UiHelper.NormalFont, Location = new Point(360, 40), AutoSize = true };
            lblSaleDate = new Label { Text = "Sale date: —", Font = UiHelper.NormalFont, Location = new Point(360, 64), AutoSize = true };
            top.Controls.Add(lblReturnNo);
            top.Controls.Add(lblCustomer);
            top.Controls.Add(lblSaleDate);
            this.Controls.Add(top);

            Panel foot = new Panel { Dock = DockStyle.Bottom, Height = 100, BackColor = Color.FromArgb(253, 242, 233) };
            foot.Controls.Add(new Label { Text = "Return reason (required)", Font = UiHelper.SmallFont, Location = new Point(16, 10), AutoSize = true });
            txtReason = new TextBox { Location = new Point(16, 30), Size = new Size(400, 28) };
            UiHelper.StyleTextBox(txtReason);
            foot.Controls.Add(txtReason);

            foot.Controls.Add(new Label { Text = "Refund amount", Font = UiHelper.SmallFont, Location = new Point(440, 10), AutoSize = true });
            txtRefund = new TextBox { Location = new Point(440, 30), Size = new Size(120, 28), Text = "0.00" };
            UiHelper.StyleTextBox(txtRefund);
            foot.Controls.Add(txtRefund);

            btnSave = new Button { Text = "SAVE RETURN (F12)", Size = new Size(160, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            Button btnClose = new Button { Text = "CLOSE (F4)", Size = new Size(120, 36), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            UiHelper.StyleAccentButton(btnSave, FormAccent.SaleReturn, FormAccent.SaleReturnDark);
            UiHelper.StyleAccentButton(btnClose, FormAccent.SaleReturnDark, FormAccent.SaleReturn);
            btnSave.Click += (s, e) => Save();
            btnClose.Click += (s, e) => this.Close();
            foot.Controls.Add(btnSave);
            foot.Controls.Add(btnClose);
            foot.Resize += (s, e) =>
            {
                btnClose.Location = new Point(foot.Width - 16 - btnClose.Width, 28);
                btnSave.Location = new Point(btnClose.Left - 10 - btnSave.Width, 28);
            };
            this.Controls.Add(foot);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.SaleReturn);
            dgv.ReadOnly = false;
            dgv.AllowUserToAddRows = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.Controls.Add(dgv);
            dgv.BringToFront();

            dgv.CellEndEdit += (s, e) => RecalcRefund();
        }

        private void LoadInvoice()
        {
            try
            {
                string inv = txtInvoice.Text.Trim();
                _invoiceLines = _service.LoadInvoice(inv);
                var first = _invoiceLines[0];
                _saleId = first.SaleID;
                _customerId = first.CustomerID;
                _customerName = first.CustomerName;
                _invoiceNo = first.InvoiceNo;

                lblCustomer.Text = "Customer: " + _customerName;
                lblSaleDate.Text = "Sale date: " + first.SaleDate.ToString("dd MMM yyyy HH:mm");
                lblReturnNo.Text = "Return No: (auto on save)";

                BuildGrid();
                RecalcRefund();
                txtReason.Focus();
            }
            catch (Exception ex)
            {
                DialogHelpers.Error(this, ex.Message);
                _invoiceLines.Clear();
                dgv.DataSource = null;
            }
        }

        private void BuildGrid()
        {
            var rows = _invoiceLines.Select(l => new ReturnGridRow
            {
                SaleDetailID = l.SaleDetailID,
                ProductID = l.ProductID,
                ProductCode = l.ProductCode,
                ProductName = l.ProductName,
                SoldQty = l.SoldQty,
                AlreadyReturned = l.AlreadyReturned,
                Returnable = l.ReturnableQty,
                ReturnQty = 0,
                UnitPrice = l.SalePrice,
                LineRefund = 0
            }).ToList();

            dgv.DataSource = null;
            dgv.DataSource = rows;

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                col.ReadOnly = col.Name != "ReturnQty";
            }
            if (dgv.Columns.Contains("SaleDetailID")) dgv.Columns["SaleDetailID"].Visible = false;
            if (dgv.Columns.Contains("ProductID")) dgv.Columns["ProductID"].Visible = false;
            if (dgv.Columns.Contains("ReturnQty"))
            {
                dgv.Columns["ReturnQty"].HeaderText = "Return Qty";
                dgv.Columns["ReturnQty"].DefaultCellStyle.BackColor = Color.FromArgb(255, 243, 224);
            }
        }

        private void RecalcRefund()
        {
            if (dgv.DataSource is List<ReturnGridRow> rows)
            {
                decimal total = 0;
                foreach (var r in rows)
                {
                    if (r.ReturnQty < 0) r.ReturnQty = 0;
                    if (r.ReturnQty > r.Returnable) r.ReturnQty = r.Returnable;
                    r.LineRefund = Math.Round(r.ReturnQty * r.UnitPrice, 2);
                    total += r.LineRefund;
                }
                txtRefund.Text = total.ToString("0.00");
                dgv.Refresh();
            }
        }

        private void Save()
        {
            if (_saleId <= 0 || _invoiceLines == null || _invoiceLines.Count == 0)
            {
                DialogHelpers.Error(this, "Load an original invoice first (F5).");
                return;
            }
            if (!(dgv.DataSource is List<ReturnGridRow> rows))
            {
                DialogHelpers.Error(this, "Nothing to return.");
                return;
            }

            string reason = txtReason.Text.Trim();
            if (string.IsNullOrEmpty(reason))
            {
                DialogHelpers.Error(this, "Return reason is required.");
                txtReason.Focus();
                return;
            }

            RecalcRefund();

            var details = new List<SaleReturnDetail>();
            foreach (var r in rows)
            {
                if (r.ReturnQty <= 0) continue;
                details.Add(new SaleReturnDetail
                {
                    OriginalSaleDetailID = r.SaleDetailID,
                    ProductID = r.ProductID,
                    ProductCode = r.ProductCode,
                    ProductName = r.ProductName,
                    Quantity = r.ReturnQty,
                    SalePrice = r.UnitPrice,
                    Amount = r.LineRefund,
                    SoldQty = r.SoldQty,
                    AlreadyReturned = r.AlreadyReturned
                });
            }

            if (details.Count == 0)
            {
                DialogHelpers.Error(this, "Enter Return Qty on at least one line.");
                return;
            }

            decimal refund = 0;
            decimal.TryParse(txtRefund.Text, out refund);

            if (!DialogHelpers.Confirm(this,
                    "Save sale return against " + _invoiceNo + "?\nLines: " + details.Count +
                    "\nRefund: " + refund.ToString("0.00") +
                    "\nReason: " + reason))
                return;

            try
            {
                var header = new SaleReturnHeader
                {
                    OriginalSaleID = _saleId,
                    OriginalInvoiceNo = _invoiceNo,
                    CustomerID = _customerId,
                    CustomerName = _customerName,
                    Discount = 0,
                    RefundAmount = refund,
                    Remarks = reason,
                    Details = details
                };
                _service.Save(header);
                DialogHelpers.Info(this, "Return saved.\nReturn No: " + header.ReturnNo + "\nStock increased.");
                this.Tag = "NOSAVECONFIRM";
                LoadInvoice();
                txtReason.Clear();
            }
            catch (Exception ex)
            {
                AppLog.Error("Sale return save", ex);
                DialogHelpers.Error(this, ex.Message);
            }
        }

        private class ReturnGridRow
        {
            public int SaleDetailID { get; set; }
            public int ProductID { get; set; }
            public string ProductCode { get; set; }
            public string ProductName { get; set; }
            public int SoldQty { get; set; }
            public int AlreadyReturned { get; set; }
            public int Returnable { get; set; }
            public int ReturnQty { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal LineRefund { get; set; }
        }
    }
}
