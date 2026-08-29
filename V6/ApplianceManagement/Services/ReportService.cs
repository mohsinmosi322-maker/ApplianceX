using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Services
{
    public class ProfitRow
    {
        public DateTime SaleDate { get; set; }
        public string InvoiceNo { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Qty { get; set; }
        public decimal SaleAmount { get; set; }
        public decimal Cogs { get; set; }
        public decimal GrossProfit { get; set; }
    }

    public class ReportService
    {
        /// <summary>
        /// Gross profit = revenue − COGS.
        /// COGS always uses pack-aware unit cost: PurchasePrice / PackSize (never full pack price × unit qty).
        /// </summary>
        public List<ProfitRow> GetProfit(DateTime from, DateTime to)
        {
            var list = new List<ProfitRow>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT h.SaleDate, h.InvoiceNo, p.ProductCode, p.ProductName, d.Quantity, d.Amount, " +
                    "ISNULL(p.PurchasePrice,0) AS PurchasePrice, ISNULL(p.PackSize,1) AS PackSize " +
                    "FROM SaleDetail d " +
                    "INNER JOIN SaleHeader h ON d.SaleID = h.SaleID " +
                    "INNER JOIN Products p ON d.ProductID = p.ProductID " +
                    "WHERE CAST(h.SaleDate AS DATE) BETWEEN @F AND @T " +
                    "ORDER BY h.SaleDate DESC";
                using (var cmd = DbHelper.CreateCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@F", from.Date);
                    cmd.Parameters.AddWithValue("@T", to.Date);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                        {
                            int qty = Convert.ToInt32(r["Quantity"]);
                            decimal amount = Convert.ToDecimal(r["Amount"]);
                            decimal packPrice = Convert.ToDecimal(r["PurchasePrice"]);
                            decimal packSize = Convert.ToDecimal(r["PackSize"]);
                            decimal unitCost = PackMath.UnitCost(packPrice, packSize);
                            decimal cogs = Math.Round(unitCost * qty, 2);
                            list.Add(new ProfitRow
                            {
                                SaleDate = (DateTime)r["SaleDate"],
                                InvoiceNo = r["InvoiceNo"].ToString(),
                                ProductCode = r["ProductCode"].ToString(),
                                ProductName = r["ProductName"].ToString(),
                                Qty = qty,
                                SaleAmount = amount,
                                Cogs = cogs,
                                GrossProfit = Math.Round(amount - cogs, 2)
                            });
                        }
                }
            }
            return list;
        }

        public List<SaleReturnReportRow> GetSaleReturns(DateTime from, DateTime to)
        {
            var list = new List<SaleReturnReportRow>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                try
                {
                    using (var cmd = DbHelper.CreateCommand(
                        "SELECT h.ReturnDate, h.ReturnNo, c.CustomerName, h.NetAmount, h.Remarks " +
                        "FROM SaleReturnHeader h LEFT JOIN Customers c ON h.CustomerID=c.CustomerID " +
                        "WHERE CAST(h.ReturnDate AS DATE) BETWEEN @F AND @T ORDER BY h.ReturnDate DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@F", from.Date);
                        cmd.Parameters.AddWithValue("@T", to.Date);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                list.Add(new SaleReturnReportRow
                                {
                                    ReturnDate = (DateTime)r["ReturnDate"],
                                    ReturnNo = r["ReturnNo"].ToString(),
                                    Customer = r["CustomerName"] == DBNull.Value ? "" : r["CustomerName"].ToString(),
                                    NetAmount = Convert.ToDecimal(r["NetAmount"]),
                                    Remarks = r["Remarks"] == DBNull.Value ? "" : r["Remarks"].ToString()
                                });
                    }
                }
                catch (SqlException) { }
            }
            return list;
        }
    }

    public class SaleReturnReportRow
    {
        public DateTime ReturnDate { get; set; }
        public string ReturnNo { get; set; }
        public string Customer { get; set; }
        public decimal NetAmount { get; set; }
        public string Remarks { get; set; }
    }
}
