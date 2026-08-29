using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Forms
{
    public partial class ProductHistoryForm : Form
    {
        public ProductHistoryForm(Product product, bool saleHistory)
        {
            this.Text = (saleHistory ? "Sale History — " : "Purchase History — ") + product.ProductCode + " " + product.ProductName;
            this.Size = new Size(720, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            this.Tag = "NOSAVECONFIRM"; // never ask confirm on close

            // ESC / F4 close silently — no exit dialog
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape || e.KeyCode == Keys.F4)
                {
                    e.SuppressKeyPress = true;
                    this.Close();
                }
            };

            Panel banner = UiHelper.CreateFormBanner(
                saleHistory ? "SALE HISTORY" : "PURCHASE HISTORY",
                product.ProductCode + " — " + product.ProductName + "  ·  ESC or F4 to close",
                saleHistory ? FormAccent.Sale : FormAccent.Purchase,
                saleHistory ? FormAccent.SaleDark : FormAccent.PurchaseDark);
            this.Controls.Add(banner);

            var dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, saleHistory ? FormAccent.Sale : FormAccent.Purchase);
            this.Controls.Add(dgv);
            dgv.BringToFront();

            try
            {
                if (saleHistory)
                {
                    var rows = new SaleRepository().GetProductSaleHistory(product.ProductID);
                    dgv.DataSource = rows;
                    if (rows == null || rows.Count == 0)
                        MessageBox.Show("No sale history for this product.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    var rows = new PurchaseRepository().GetProductPurchaseHistory(product.ProductID);
                    dgv.DataSource = rows;
                    if (rows == null || rows.Count == 0)
                        MessageBox.Show("No purchase history for this product.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load history:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    public class ProductSaleHistoryRow
    {
        public DateTime Date { get; set; }
        public string Invoice { get; set; }
        public string Customer { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
    }

    public class ProductPurchaseHistoryRow
    {
        public DateTime Date { get; set; }
        public string Invoice { get; set; }
        public string Supplier { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
    }
}
