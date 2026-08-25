using System;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Forms
{
    public partial class ReportsForm : Form
    {
        private string reportType;
        private DateTimePicker dtFrom, dtTo;
        private DataGridView dgv;
        private Label lblSummary;

        public ReportsForm(string type)
        {
            reportType = type;
            this.Text = type + " Report";
            this.Size = new Size(1020, 640);
            this.BackColor = UiHelper.BgColor;
            this.KeyPreview = true;
            this.Padding = new Padding(12);
            UiHelper.AttachF4Close(this);

            Panel top = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White, Padding = new Padding(12, 10, 12, 8) };
            top.Controls.Add(new Label { Text = "From", Font = UiHelper.NormalFont, Location = new Point(12, 16), AutoSize = true });
            dtFrom = new DateTimePicker { Location = new Point(58, 12), Size = new Size(130, 26), Value = DateTime.Today.AddDays(-30) };
            UiHelper.StyleDatePicker(dtFrom);
            top.Controls.Add(dtFrom);
            top.Controls.Add(new Label { Text = "To", Font = UiHelper.NormalFont, Location = new Point(200, 16), AutoSize = true });
            dtTo = new DateTimePicker { Location = new Point(228, 12), Size = new Size(130, 26), Value = DateTime.Today };
            UiHelper.StyleDatePicker(dtTo);
            top.Controls.Add(dtTo);
            Button btnView = new Button { Text = "VIEW", Location = new Point(380, 10), Size = new Size(110, 34) };
            UiHelper.StyleButton(btnView);
            btnView.Click += (s, e) => LoadReport();
            top.Controls.Add(btnView);
            this.Controls.Add(top);

            Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.White };
            lblSummary = new Label { Dock = DockStyle.Fill, Font = UiHelper.HeaderFont, ForeColor = UiHelper.ThemeColor, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0) };
            bottom.Controls.Add(lblSummary);
            this.Controls.Add(bottom);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGrid(dgv);
            this.Controls.Add(dgv);
            dgv.BringToFront();
            LoadReport();
        }

        private void LoadReport()
        {
            try
            {
                if (reportType == "SALES")
                {
                    var data = new SaleRepository().GetSales(dtFrom.Value, dtTo.Value);
                    dgv.DataSource = data;
                    Hide("SaleID", "CustomerID", "Remarks", "Details");
                    decimal t = 0; foreach (var s in data) t += s.NetAmount;
                    lblSummary.Text = "Sales: " + data.Count + "    |    Net: " + t.ToString("N2");
                }
                else if (reportType == "PURCHASE")
                {
                    var data = new PurchaseRepository().GetPurchases(dtFrom.Value, dtTo.Value);
                    dgv.DataSource = data;
                    Hide("PurchaseID", "SupplierID", "Remarks", "Details");
                    decimal t = 0; foreach (var p in data) t += p.NetAmount;
                    lblSummary.Text = "Purchases: " + data.Count + "    |    Net: " + t.ToString("N2");
                }
                else if (reportType == "STOCK")
                {
                    var data = new ProductRepository().GetAllActive();
                    dgv.DataSource = data;
                    Hide("ProductID", "CategoryID", "IsActive", "CreatedDate");
                    decimal val = 0; int units = 0;
                    foreach (var p in data) { units += p.CurrentStock; val += p.StockValue; }
                    lblSummary.Text = "Products: " + data.Count + "    |    Units: " + units + "    |    Stock Value: " + val.ToString("N2");
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.Cells["SalePrice"].Value == null || row.Cells["PurchasePrice"].Value == null) continue;
                        decimal sp = Convert.ToDecimal(row.Cells["SalePrice"].Value);
                        decimal pp = Convert.ToDecimal(row.Cells["PurchasePrice"].Value);
                        if (sp < pp) row.DefaultCellStyle.BackColor = Color.FromArgb(255, 205, 210);
                        else if (sp > pp) row.DefaultCellStyle.BackColor = Color.FromArgb(200, 230, 201);
                    }
                }
                else if (reportType == "PROFIT")
                {
                    var sales = new SaleRepository().GetSales(dtFrom.Value, dtTo.Value);
                    dgv.DataSource = sales;
                    Hide("SaleID", "CustomerID", "Remarks", "Details", "PaidAmount", "BalanceAmount");
                    decimal t = 0; foreach (var s in sales) t += s.NetAmount;
                    lblSummary.Text = "Sales: " + sales.Count + "    |    Total Net Amount: " + t.ToString("N2") + "    (COGS profit after migration)";
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void Hide(params string[] names)
        {
            foreach (var n in names)
                if (dgv.Columns.Contains(n)) dgv.Columns[n].Visible = false;
        }
    }
}
