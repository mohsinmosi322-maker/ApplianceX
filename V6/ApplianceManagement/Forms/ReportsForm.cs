using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

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
            UiHelper.AttachF4Close(this);

            this.Controls.Add(UiHelper.CreateFormBanner(
                type + " REPORT",
                type == "SALES" ? "Green = Sale  ·  Red = Sale Return" : "Date range filter  ·  F4 close",
                FormAccent.Reports, FormAccent.ReportsDark));

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
            UiHelper.StyleAccentButton(btnView, FormAccent.Reports, FormAccent.ReportsDark);
            btnView.Click += (s, e) => LoadReport();
            top.Controls.Add(btnView);
            this.Controls.Add(top);

            Panel bottom = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.White };
            lblSummary = new Label { Dock = DockStyle.Fill, Font = UiHelper.HeaderFont, ForeColor = FormAccent.Reports, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 0, 0, 0) };
            bottom.Controls.Add(lblSummary);
            this.Controls.Add(bottom);

            dgv = new DataGridView { Dock = DockStyle.Fill };
            UiHelper.StyleGridWithAccent(dgv, FormAccent.Reports);
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
                    var table = new DataTable();
                    table.Columns.Add("Type", typeof(string));
                    table.Columns.Add("Date", typeof(DateTime));
                    table.Columns.Add("Invoice", typeof(string));
                    table.Columns.Add("Customer", typeof(string));
                    table.Columns.Add("NetAmount", typeof(decimal));
                    table.Columns.Add("Remarks", typeof(string));

                    var sales = new SaleRepository().GetSales(dtFrom.Value, dtTo.Value);
                    foreach (var s in sales)
                        table.Rows.Add("SALE", s.SaleDate, s.InvoiceNo, s.CustomerName, s.NetAmount, "");

                    // Sale returns from inventory ledger
                    using (var conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = DbHelper.CreateCommand(
                            "SELECT TransactionDate, ProductID, QuantityIn, UnitCost, Remarks " +
                            "FROM InventoryTransaction WHERE TransactionType=@T " +
                            "AND CAST(TransactionDate AS DATE) BETWEEN @F AND @T ORDER BY TransactionDate DESC", conn))
                        {
                            cmd.Parameters.AddWithValue("@T", InventoryTransactionType.SaleReturn);
                            cmd.Parameters.AddWithValue("@F", dtFrom.Value.Date);
                            cmd.Parameters.AddWithValue("@T2", dtTo.Value.Date);
                            // fix param name
                        }
                    }
                    // re-run returns with correct params
                    using (var conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        using (var cmd = DbHelper.CreateCommand(
                            "SELECT t.TransactionDate, p.ProductCode, p.ProductName, t.QuantityIn, t.UnitCost, t.Remarks " +
                            "FROM InventoryTransaction t LEFT JOIN Products p ON t.ProductID=p.ProductID " +
                            "WHERE t.TransactionType=@Typ AND CAST(t.TransactionDate AS DATE) BETWEEN @F AND @ToD ORDER BY t.TransactionDate DESC", conn))
                        {
                            cmd.Parameters.AddWithValue("@Typ", InventoryTransactionType.SaleReturn);
                            cmd.Parameters.AddWithValue("@F", dtFrom.Value.Date);
                            cmd.Parameters.AddWithValue("@ToD", dtTo.Value.Date);
                            using (var r = cmd.ExecuteReader())
                                while (r.Read())
                                {
                                    decimal amt = Convert.ToDecimal(r["QuantityIn"]) * Convert.ToDecimal(r["UnitCost"]);
                                    string prod = (r["ProductCode"] == DBNull.Value ? "" : r["ProductCode"].ToString()) + " " +
                                                  (r["ProductName"] == DBNull.Value ? "" : r["ProductName"].ToString());
                                    table.Rows.Add("RETURN", (DateTime)r["TransactionDate"], "RET-", prod.Trim(), amt,
                                        r["Remarks"] == DBNull.Value ? "" : r["Remarks"].ToString());
                                }
                        }
                    }

                    dgv.DataSource = table;
                    decimal saleTot = 0, retTot = 0; int sc = 0, rc = 0;
                    foreach (DataRow row in table.Rows)
                    {
                        if (row["Type"].ToString() == "SALE") { saleTot += (decimal)row["NetAmount"]; sc++; }
                        else { retTot += (decimal)row["NetAmount"]; rc++; }
                    }
                    lblSummary.Text = "Sales: " + sc + " (" + saleTot.ToString("N2") + ")    |    Returns: " + rc + " (" + retTot.ToString("N2") + ")";

                    // Color rows: green sale, red return
                    dgv.DataBindingComplete += (s, e) =>
                    {
                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            if (row.Cells["Type"].Value == null) continue;
                            string typ = row.Cells["Type"].Value.ToString();
                            if (typ == "SALE")
                                row.DefaultCellStyle.ForeColor = Color.FromArgb(27, 94, 32);
                            else if (typ == "RETURN")
                                row.DefaultCellStyle.ForeColor = Color.FromArgb(183, 28, 28);
                        }
                    };
                    foreach (DataGridViewRow row in dgv.Rows)
                    {
                        if (row.Cells["Type"].Value == null) continue;
                        string typ = row.Cells["Type"].Value.ToString();
                        if (typ == "SALE") row.DefaultCellStyle.ForeColor = Color.FromArgb(27, 94, 32);
                        else if (typ == "RETURN") row.DefaultCellStyle.ForeColor = Color.FromArgb(183, 28, 28);
                    }
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
                    lblSummary.Text = "Sales: " + sales.Count + "    |    Total Net Amount: " + t.ToString("N2");
                    foreach (DataGridViewRow row in dgv.Rows)
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(27, 94, 32);
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
