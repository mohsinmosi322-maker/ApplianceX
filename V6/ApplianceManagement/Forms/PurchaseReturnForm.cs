using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ApplianceManagement.Helpers;
using ApplianceManagement.Services;

namespace ApplianceManagement.Forms
{
    public partial class PurchaseReturnForm : Form
    {
        private readonly PurchaseReturnService _service = new PurchaseReturnService();
        private List<PurchaseInvoiceLine> _lines = new List<PurchaseInvoiceLine>();
        private int _purchaseId, _supplierId;
        private string _invoiceNo = "", _supplierName = "";
        private TextBox txtInvoice, txtReason, txtRefund;
        private Label lblInfo;
        private DataGridView dgv;

        public PurchaseReturnForm()
        {
            Text = "Purchase Return";
            Size = new Size(1100, 680);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this);
            KeyDown += (s, e) => { if (e.KeyCode == Keys.F5) LoadInv(); if (e.KeyCode == Keys.F12) Save(); };

            Controls.Add(UiHelper.CreateFormBanner("PURCHASE RETURN",
                "Load PUR invoice · Return packs ≤ purchased − returned · Reason required",
                FormAccent.Purchase, FormAccent.PurchaseDark));

            var top = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.White };
            top.Controls.Add(new Label { Text = "Original Purchase Invoice", Location = new Point(16, 12), AutoSize = true, Font = UiHelper.SmallFont });
            txtInvoice = new TextBox { Location = new Point(16, 32), Size = new Size(200, 28) };
            UiHelper.StyleTextBox(txtInvoice);
            txtInvoice.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadInv(); } };
            top.Controls.Add(txtInvoice);
            var btn = new Button { Text = "LOAD (F5)", Location = new Point(230, 30), Size = new Size(110, 32) };
            UiHelper.StyleAccentButton(btn, FormAccent.Purchase, FormAccent.PurchaseDark);
            btn.Click += (s, e) => LoadInv();
            top.Controls.Add(btn);
            lblInfo = new Label { Text = "—", Location = new Point(360, 34), AutoSize = true, Font = UiHelper.NormalFont };
            top.Controls.Add(lblInfo);
            Controls.Add(top);

            var foot = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Color.FromArgb(232, 248, 238) };
            foot.Controls.Add(new Label { Text = "Reason", Location = new Point(16, 10), AutoSize = true, Font = UiHelper.SmallFont });
            txtReason = new TextBox { Location = new Point(16, 30), Size = new Size(360, 28) };
            UiHelper.StyleTextBox(txtReason);
            foot.Controls.Add(txtReason);
            foot.Controls.Add(new Label { Text = "Refund", Location = new Point(400, 10), AutoSize = true, Font = UiHelper.SmallFont });
            txtRefund = new TextBox { Location = new Point(400, 30), Size = new Size(120, 28), Text = "0.00" };
            UiHelper.StyleTextBox(txtRefund);
            foot.Controls.Add(txtRefund);
            var save = new Button { Text = "SAVE (F12)", Size = new Size(140, 34), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            UiHelper.StyleAccentButton(save, FormAccent.Purchase, FormAccent.PurchaseDark);
            save.Click += (s, e) => Save();
            foot.Controls.Add(save);
            foot.Resize += (s, e) => save.Location = new Point(foot.Width - 16 - save.Width, 28);
            Controls.Add(foot);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.Purchase);
            dgv.ReadOnly = false;
            dgv.AllowUserToAddRows = false;
            Controls.Add(dgv);
            dgv.BringToFront();
            dgv.CellEndEdit += (s, e) => Recalc();
        }

        private void LoadInv()
        {
            try
            {
                _lines = _service.LoadInvoice(txtInvoice.Text.Trim());
                var f = _lines[0];
                _purchaseId = f.PurchaseID;
                _supplierId = f.SupplierID;
                _invoiceNo = f.InvoiceNo;
                _supplierName = f.SupplierName;
                lblInfo.Text = "Supplier: " + _supplierName + "  |  " + f.PurchaseDate.ToString("dd MMM yyyy");
                var rows = _lines.Select(l => new Row
                {
                    PurchaseDetailID = l.PurchaseDetailID,
                    ProductID = l.ProductID,
                    ProductCode = l.ProductCode,
                    ProductName = l.ProductName,
                    Purchased = l.PurchasedQty,
                    AlreadyReturned = l.AlreadyReturned,
                    Returnable = l.ReturnableQty,
                    ReturnQty = 0,
                    PackPrice = l.PurchasePrice,
                    PackSize = l.PackSize,
                    Amount = 0
                }).ToList();
                dgv.DataSource = rows;
                foreach (DataGridViewColumn c in dgv.Columns)
                    c.ReadOnly = c.Name != "ReturnQty";
                if (dgv.Columns.Contains("PurchaseDetailID")) dgv.Columns["PurchaseDetailID"].Visible = false;
                if (dgv.Columns.Contains("ProductID")) dgv.Columns["ProductID"].Visible = false;
                Recalc();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void Recalc()
        {
            if (!(dgv.DataSource is List<Row> rows)) return;
            decimal t = 0;
            foreach (var r in rows)
            {
                if (r.ReturnQty < 0) r.ReturnQty = 0;
                if (r.ReturnQty > r.Returnable) r.ReturnQty = r.Returnable;
                r.Amount = Math.Round(r.ReturnQty * r.PackPrice, 2);
                t += r.Amount;
            }
            txtRefund.Text = t.ToString("0.00");
            dgv.Refresh();
        }

        private void Save()
        {
            if (_purchaseId <= 0) { MessageBox.Show("Load invoice first."); return; }
            if (string.IsNullOrWhiteSpace(txtReason.Text)) { MessageBox.Show("Reason required."); return; }
            if (!(dgv.DataSource is List<Row> rows)) return;
            Recalc();
            var details = new List<PurchaseReturnDetail>();
            foreach (var r in rows)
            {
                if (r.ReturnQty <= 0) continue;
                details.Add(new PurchaseReturnDetail
                {
                    OriginalPurchaseDetailID = r.PurchaseDetailID,
                    ProductID = r.ProductID,
                    ProductCode = r.ProductCode,
                    ProductName = r.ProductName,
                    Quantity = r.ReturnQty,
                    PurchasePrice = r.PackPrice,
                    Amount = r.Amount,
                    PurchasedQty = r.Purchased,
                    AlreadyReturned = r.AlreadyReturned,
                    PackSize = r.PackSize
                });
            }
            if (details.Count == 0) { MessageBox.Show("Enter return qty."); return; }
            decimal refund = 0; decimal.TryParse(txtRefund.Text, out refund);
            if (MessageBox.Show("Save purchase return?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            try
            {
                var h = new PurchaseReturnHeader
                {
                    OriginalPurchaseID = _purchaseId,
                    OriginalInvoiceNo = _invoiceNo,
                    SupplierID = _supplierId,
                    SupplierName = _supplierName,
                    RefundAmount = refund,
                    Remarks = txtReason.Text.Trim(),
                    Details = details
                };
                _service.Save(h);
                MessageBox.Show("Saved.\nReturn No: " + h.ReturnNo);
                Tag = "NOSAVECONFIRM";
                LoadInv();
                txtReason.Clear();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private class Row
        {
            public int PurchaseDetailID { get; set; }
            public int ProductID { get; set; }
            public string ProductCode { get; set; }
            public string ProductName { get; set; }
            public int Purchased { get; set; }
            public int AlreadyReturned { get; set; }
            public int Returnable { get; set; }
            public int ReturnQty { get; set; }
            public decimal PackPrice { get; set; }
            public decimal PackSize { get; set; }
            public decimal Amount { get; set; }
        }
    }
}
