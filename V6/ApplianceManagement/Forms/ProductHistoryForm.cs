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
            UiHelper.AttachF4Close(this);

            var dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGrid(dgv);
            this.Controls.Add(dgv);

            if (saleHistory)
            {
                var rows = new SaleRepository().GetProductSaleHistory(product.ProductID);
                dgv.DataSource = rows;
            }
            else
            {
                var rows = new PurchaseRepository().GetProductPurchaseHistory(product.ProductID);
                dgv.DataSource = rows;
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
