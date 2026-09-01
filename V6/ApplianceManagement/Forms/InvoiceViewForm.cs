using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    /// <summary>
    /// Thermal-style invoice preview by invoice number (Sale or Purchase).
    /// </summary>
    public class InvoiceViewForm : Form
    {
        private TextBox txtInvoice;
        private ComboBox cmbType;
        private RichTextBox preview;
        private Button btnLoad;

        public InvoiceViewForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Invoice View";
            Size = new Size(480, 640);
            BackColor = UiHelper.BgColor;
            KeyPreview = true;
            UiHelper.AttachF4Close(this, false);
            UiHelper.AttachEnterNavigation(this);

            Controls.Add(UiHelper.CreateFormBanner(
                "INVOICE VIEW",
                "Enter invoice number · thermal preview · Sale or Purchase",
                FormAccent.Reports, FormAccent.ReportsDark));

            var top = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White, Padding = new Padding(12, 10, 12, 8) };
            top.Controls.Add(new Label { Text = "Type", Location = new Point(8, 16), AutoSize = true, Font = UiHelper.SmallFont });
            cmbType = new ComboBox
            {
                Location = new Point(48, 12),
                Size = new Size(110, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbType.Items.AddRange(new object[] { "Sale", "Purchase" });
            cmbType.SelectedIndex = 0;
            UiHelper.StyleComboBox(cmbType);
            top.Controls.Add(cmbType);

            top.Controls.Add(new Label { Text = "Invoice #", Location = new Point(170, 16), AutoSize = true, Font = UiHelper.SmallFont });
            txtInvoice = new TextBox { Location = new Point(240, 12), Size = new Size(120, 28) };
            UiHelper.StyleTextBox(txtInvoice);
            top.Controls.Add(txtInvoice);

            btnLoad = new Button { Text = "VIEW (F5)", Location = new Point(370, 10), Size = new Size(90, 32) };
            UiHelper.StyleAccentButton(btnLoad, FormAccent.Reports, FormAccent.ReportsDark);
            btnLoad.Click += (s, e) => LoadInvoice();
            top.Controls.Add(btnLoad);
            Controls.Add(top);

            preview = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10F),
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(20)
            };
            Controls.Add(preview);

            // Dock order: Fill first, then Top panels
            Controls.SetChildIndex(preview, 0);

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F5) { LoadInvoice(); e.Handled = true; }
            };

            Shown += (s, e) => txtInvoice.Focus();
        }

        private void LoadInvoice()
        {
            string inv = (txtInvoice.Text ?? "").Trim();
            if (string.IsNullOrEmpty(inv))
            {
                DialogHelpers.Warn(this, "Enter invoice number.");
                txtInvoice.Focus();
                return;
            }

            bool isSale = cmbType.SelectedIndex == 0;
            try
            {
                string text = isSale ? BuildSaleThermal(inv) : BuildPurchaseThermal(inv);
                preview.Text = text;
            }
            catch (Exception ex)
            {
                preview.Text = "";
                DialogHelpers.Error(this, ex.Message);
            }
        }

        private string BuildSaleThermal(string invoiceNo)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                int saleId = 0;
                DateTime date = DateTime.MinValue;
                string customer = "";
                decimal total = 0, disc = 0, net = 0, paid = 0, bal = 0;

                using (var cmd = DbHelper.CreateCommand(
                    "SELECT h.SaleID, h.SaleDate, ISNULL(c.CustomerName,'Walk-in') AS CustomerName, " +
                    "h.TotalAmount, h.Discount, h.NetAmount, h.PaidAmount, h.BalanceAmount " +
                    "FROM SaleHeader h LEFT JOIN Customers c ON h.CustomerID=c.CustomerID " +
                    "WHERE h.InvoiceNo=@Inv", conn))
                {
                    cmd.Parameters.AddWithValue("@Inv", invoiceNo);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new InvalidOperationException("Invalid invoice: " + invoiceNo);
                        saleId = (int)r["SaleID"];
                        date = (DateTime)r["SaleDate"];
                        customer = r["CustomerName"].ToString();
                        total = Convert.ToDecimal(r["TotalAmount"]);
                        disc = Convert.ToDecimal(r["Discount"]);
                        net = Convert.ToDecimal(r["NetAmount"]);
                        paid = Convert.ToDecimal(r["PaidAmount"]);
                        bal = Convert.ToDecimal(r["BalanceAmount"]);
                    }
                }

                var sb = new StringBuilder();
                sb.AppendLine(Center(UiHelper.GetShopName(), 42));
                sb.AppendLine(Center("SALE INVOICE", 42));
                sb.AppendLine(new string('-', 42));
                sb.AppendLine("Inv #: " + invoiceNo);
                sb.AppendLine("Date : " + date.ToString("dd/MM/yyyy HH:mm"));
                sb.AppendLine("Cust : " + customer);
                sb.AppendLine(new string('-', 42));
                sb.AppendLine(Pad("Item", 18) + PadR("Qty", 6) + PadR("Rate", 9) + PadR("Amt", 9));
                sb.AppendLine(new string('-', 42));

                using (var cmd = DbHelper.CreateCommand(
                    "SELECT p.ProductName, d.Quantity, d.SalePrice, d.Amount " +
                    "FROM SaleDetail d INNER JOIN Products p ON d.ProductID=p.ProductID " +
                    "WHERE d.SaleID=@Id ORDER BY d.SaleDetailID", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", saleId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string name = r["ProductName"].ToString();
                            if (name.Length > 18) name = name.Substring(0, 16) + "..";
                            int qty = Convert.ToInt32(r["Quantity"]);
                            decimal rate = Convert.ToDecimal(r["SalePrice"]);
                            decimal amt = Convert.ToDecimal(r["Amount"]);
                            sb.AppendLine(Pad(name, 18) + PadR(qty.ToString(), 6) + PadR(rate.ToString("0.00"), 9) + PadR(amt.ToString("0.00"), 9));
                        }
                    }
                }

                sb.AppendLine(new string('-', 42));
                sb.AppendLine(PadR("Total:", 33) + PadR(total.ToString("0.00"), 9));
                sb.AppendLine(PadR("Discount:", 33) + PadR(disc.ToString("0.00"), 9));
                sb.AppendLine(PadR("Net:", 33) + PadR(net.ToString("0.00"), 9));
                sb.AppendLine(PadR("Paid:", 33) + PadR(paid.ToString("0.00"), 9));
                sb.AppendLine(PadR("Balance:", 33) + PadR(bal.ToString("0.00"), 9));
                sb.AppendLine(new string('-', 42));
                sb.AppendLine(Center("Thank you", 42));
                return sb.ToString();
            }
        }

        private string BuildPurchaseThermal(string invoiceNo)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                int purchaseId = 0;
                DateTime date = DateTime.MinValue;
                string supplier = "";
                decimal total = 0, disc = 0, net = 0, paid = 0, bal = 0;

                using (var cmd = DbHelper.CreateCommand(
                    "SELECT h.PurchaseID, h.PurchaseDate, s.SupplierName, " +
                    "h.TotalAmount, h.Discount, h.NetAmount, h.PaidAmount, h.BalanceAmount " +
                    "FROM PurchaseHeader h INNER JOIN Suppliers s ON h.SupplierID=s.SupplierID " +
                    "WHERE h.InvoiceNo=@Inv", conn))
                {
                    cmd.Parameters.AddWithValue("@Inv", invoiceNo);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                            throw new InvalidOperationException("Invalid invoice: " + invoiceNo);
                        purchaseId = (int)r["PurchaseID"];
                        date = (DateTime)r["PurchaseDate"];
                        supplier = r["SupplierName"].ToString();
                        total = Convert.ToDecimal(r["TotalAmount"]);
                        disc = Convert.ToDecimal(r["Discount"]);
                        net = Convert.ToDecimal(r["NetAmount"]);
                        paid = Convert.ToDecimal(r["PaidAmount"]);
                        bal = Convert.ToDecimal(r["BalanceAmount"]);
                    }
                }

                var sb = new StringBuilder();
                sb.AppendLine(Center(UiHelper.GetShopName(), 42));
                sb.AppendLine(Center("PURCHASE INVOICE", 42));
                sb.AppendLine(new string('-', 42));
                sb.AppendLine("Inv #: " + invoiceNo);
                sb.AppendLine("Date : " + date.ToString("dd/MM/yyyy HH:mm"));
                sb.AppendLine("Supp : " + supplier);
                sb.AppendLine(new string('-', 42));
                sb.AppendLine(Pad("Item", 18) + PadR("Qty", 6) + PadR("Rate", 9) + PadR("Amt", 9));
                sb.AppendLine(new string('-', 42));

                using (var cmd = DbHelper.CreateCommand(
                    "SELECT p.ProductName, d.Quantity, d.PurchasePrice, d.Amount " +
                    "FROM PurchaseDetail d INNER JOIN Products p ON d.ProductID=p.ProductID " +
                    "WHERE d.PurchaseID=@Id ORDER BY d.PurchaseDetailID", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", purchaseId);
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string name = r["ProductName"].ToString();
                            if (name.Length > 18) name = name.Substring(0, 16) + "..";
                            int qty = Convert.ToInt32(r["Quantity"]);
                            decimal rate = Convert.ToDecimal(r["PurchasePrice"]);
                            decimal amt = Convert.ToDecimal(r["Amount"]);
                            sb.AppendLine(Pad(name, 18) + PadR(qty.ToString(), 6) + PadR(rate.ToString("0.00"), 9) + PadR(amt.ToString("0.00"), 9));
                        }
                    }
                }

                sb.AppendLine(new string('-', 42));
                sb.AppendLine(PadR("Total:", 33) + PadR(total.ToString("0.00"), 9));
                sb.AppendLine(PadR("Discount:", 33) + PadR(disc.ToString("0.00"), 9));
                sb.AppendLine(PadR("Net:", 33) + PadR(net.ToString("0.00"), 9));
                sb.AppendLine(PadR("Paid:", 33) + PadR(paid.ToString("0.00"), 9));
                sb.AppendLine(PadR("Balance:", 33) + PadR(bal.ToString("0.00"), 9));
                sb.AppendLine(new string('-', 42));
                return sb.ToString();
            }
        }

        private static string Center(string s, int w)
        {
            if (s == null) s = "";
            if (s.Length >= w) return s.Substring(0, w);
            int pad = (w - s.Length) / 2;
            return new string(' ', pad) + s;
        }

        private static string Pad(string s, int w)
        {
            if (s == null) s = "";
            if (s.Length > w) return s.Substring(0, w);
            return s.PadRight(w);
        }

        private static string PadR(string s, int w)
        {
            if (s == null) s = "";
            if (s.Length > w) return s.Substring(0, w);
            return s.PadLeft(w);
        }
    }
}
