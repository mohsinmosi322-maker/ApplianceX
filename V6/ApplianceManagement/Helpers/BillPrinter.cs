using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using ApplianceManagement.Models;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Simple polished thermal-style bill print (80mm friendly layout).
    /// Controlled by AppSettings AllowBillPrint.
    /// </summary>
    public static class BillPrinter
    {
        private static SaleHeader _sale;

        public static void PrintSaleBill(SaleHeader sale)
        {
            if (sale == null || sale.Details == null || sale.Details.Count == 0) return;
            _sale = sale;

            var doc = new PrintDocument();
            doc.DocumentName = "Sale Bill";
            // Prefer narrow width for thermal; fallback to default
            try
            {
                doc.DefaultPageSettings.PaperSize = new PaperSize("Thermal80", 300, 800);
                doc.DefaultPageSettings.Margins = new Margins(10, 10, 10, 10);
            }
            catch { }

            doc.PrintPage += Doc_PrintPage;

            using (var preview = new PrintPreviewDialog())
            {
                preview.Document = doc;
                preview.Width = 500;
                preview.Height = 700;
                preview.ShowDialog();
            }
            // Also offer direct print
            if (MessageBox.Show("Send to printer?", "Print", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try { doc.Print(); }
                catch (Exception ex) { MessageBox.Show("Printer error: " + ex.Message); }
            }
        }

        private static void Doc_PrintPage(object sender, PrintPageEventArgs e)
        {
            var g = e.Graphics;
            float y = 10;
            float left = 10;
            float width = e.MarginBounds.Width > 0 ? e.MarginBounds.Width : 280;

            string shop = UiHelper.GetShopName();
            string phone = UiHelper.GetShopPhone();

            using (var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold))
            using (var normal = new Font("Segoe UI", 8f))
            using (var bold = new Font("Segoe UI", 8f, FontStyle.Bold))
            using (var small = new Font("Segoe UI", 7f))
            {
                // Header
                g.DrawString(shop, titleFont, Brushes.Black, left, y);
                y += 22;
                if (!string.IsNullOrEmpty(phone))
                {
                    g.DrawString("Tel: " + phone, small, Brushes.Black, left, y);
                    y += 14;
                }
                g.DrawLine(Pens.Black, left, y, left + width, y);
                y += 8;

                g.DrawString("SALE RECEIPT", bold, Brushes.Black, left, y);
                y += 16;
                g.DrawString("Date: " + _sale.SaleDate.ToString("dd/MM/yyyy HH:mm"), normal, Brushes.Black, left, y);
                y += 14;
                g.DrawString("Customer: Walk-in Customer", normal, Brushes.Black, left, y);
                y += 14;
                g.DrawLine(Pens.Black, left, y, left + width, y);
                y += 8;

                // Column headers
                g.DrawString("Item", bold, Brushes.Black, left, y);
                g.DrawString("Qty", bold, Brushes.Black, left + 140, y);
                g.DrawString("Amount", bold, Brushes.Black, left + 180, y);
                y += 14;
                g.DrawLine(Pens.Gray, left, y, left + width, y);
                y += 6;

                foreach (var d in _sale.Details)
                {
                    string name = d.ProductName;
                    if (name.Length > 22) name = name.Substring(0, 22);
                    g.DrawString(name, normal, Brushes.Black, left, y);
                    g.DrawString(d.Quantity.ToString(), normal, Brushes.Black, left + 145, y);
                    g.DrawString(d.Amount.ToString("0.00"), normal, Brushes.Black, left + 180, y);
                    y += 13;
                    g.DrawString("  " + d.SalePrice.ToString("0.00") + " x " + d.Quantity, small, Brushes.Gray, left, y);
                    y += 14;
                }

                g.DrawLine(Pens.Black, left, y, left + width, y);
                y += 8;
                g.DrawString("Total:", normal, Brushes.Black, left, y);
                g.DrawString(_sale.TotalAmount.ToString("0.00"), normal, Brushes.Black, left + 180, y);
                y += 14;
                if (_sale.Discount > 0)
                {
                    g.DrawString("Discount:", normal, Brushes.Black, left, y);
                    g.DrawString(_sale.Discount.ToString("0.00"), normal, Brushes.Black, left + 180, y);
                    y += 14;
                }
                g.DrawString("Net Amount:", bold, Brushes.Black, left, y);
                g.DrawString(_sale.NetAmount.ToString("0.00"), bold, Brushes.Black, left + 180, y);
                y += 14;
                g.DrawString("Paid:", normal, Brushes.Black, left, y);
                g.DrawString(_sale.PaidAmount.ToString("0.00"), normal, Brushes.Black, left + 180, y);
                y += 14;
                if (_sale.BalanceAmount != 0)
                {
                    g.DrawString("Balance:", normal, Brushes.Black, left, y);
                    g.DrawString(_sale.BalanceAmount.ToString("0.00"), normal, Brushes.Black, left + 180, y);
                    y += 14;
                }

                y += 10;
                g.DrawLine(Pens.Black, left, y, left + width, y);
                y += 10;
                g.DrawString("Thank you for your business!", small, Brushes.Black, left, y);
                y += 12;
                g.DrawString(UiHelper.AppName + " v" + UiHelper.AppVersion, small, Brushes.Gray, left, y);
            }

            e.HasMorePages = false;
        }
    }
}
