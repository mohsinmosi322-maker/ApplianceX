using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

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
            this.Tag = "NOSAVECONFIRM";

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

            Label empty = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = UiHelper.SmallFont,
                ForeColor = Color.Gray,
                Text = ""
            };
            this.Controls.Add(empty);

            try
            {
                if (saleHistory)
                {
                    var rows = new SaleService().GetProductHistory(product.ProductID);
                    dgv.DataSource = rows;
                    empty.Text = (rows == null || rows.Count == 0)
                        ? "No sale history for this product."
                        : rows.Count + " sale line(s)";
                }
                else
                {
                    var rows = new PurchaseService().GetProductHistory(product.ProductID);
                    dgv.DataSource = rows;
                    empty.Text = (rows == null || rows.Count == 0)
                        ? "No purchase history for this product."
                        : rows.Count + " purchase line(s)";
                }
            }
            catch (Exception ex)
            {
                empty.Text = "Could not load history: " + ex.Message;
                empty.ForeColor = Color.FromArgb(183, 28, 28);
            }
        }
    }
}
