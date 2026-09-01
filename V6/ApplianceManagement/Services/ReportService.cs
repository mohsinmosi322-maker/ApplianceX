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
        public decimal UnitCost { get; set; }
        public decimal Cogs { get; set; }
        public decimal GrossProfit { get; set; }
    }

    public class ReportService
    {
        /// <summary>
        /// Gross profit = revenue − COGS.
        /// Unit cost priority:
        /// 1) Weighted-average unit cost from InventoryTransaction (IN rows) — correct even if Products.PurchasePrice was corrupted
        /// 2) Latest purchase unit cost from ledger
        /// 3) PackMath: Products.PurchasePrice / PackSize (PurchasePrice must be pack price)
        /// </summary>
        public List<ProfitRow> GetProfit(DateTime from, DateTime to)
        {
            var list = new List<ProfitRow>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                string sql =
                    "SELECT h.SaleDate, h.InvoiceNo, p.ProductID, p.ProductCode, p.ProductName, d.Quantity, d.Amount, " +
                    "ISNULL(p.PurchasePrice,0) AS PurchasePrice, ISNULL(p.PackSize,1) AS PackSize, " +
                    "ISNULL((SELECT CASE WHEN SUM(t.QuantityIn)=0 THEN NULL " +
                    "  ELSE SUM(t.QuantityIn * t.UnitCost) / NULLIF(SUM(t.QuantityIn),0) END " +
                    "  FROM InventoryTransaction t WHERE t.ProductID=p.ProductID AND t.QuantityIn>0 AND t.UnitCost IS NOT NULL), NULL) AS AvgUnitCost, " +
                    "ISNULL((SELECT TOP 1 t.UnitCost FROM InventoryTransaction t " +
                    "  WHERE t.ProductID=p.ProductID AND t.QuantityIn>0 AND t.UnitCost IS NOT NULL " +
                    "  ORDER BY t.TransactionDate DESC, t.TransactionID DESC), NULL) AS LastUnitCost " +
                    "FROM SaleDetail d " +
                    "INNER JOIN SaleHeader h ON d.SaleID = h.SaleID " +
                    "INNER JOIN Products p ON d.ProductID = p.ProductID " +
                    "WHERE CAST(h.SaleDate AS DATE) BETWEEN @F AND @T " +
                    "ORDER BY h.SaleDate DESC";
                try
                {
                    using (var cmd = DbHelper.CreateCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@F", from.Date);
                        cmd.Parameters.AddWithValue("@T", to.Date);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                                list.Add(BuildProfitRow(r));
                    }
                }
                catch (SqlException)
                {
                    // Older DB without InventoryTransaction columns — pack-aware fallback only
                    using (var cmd = DbHelper.CreateCommand(
                        "SELECT h.SaleDate, h.InvoiceNo, p.ProductCode, p.ProductName, d.Quantity, d.Amount, " +
                        "ISNULL(p.PurchasePrice,0) AS PurchasePrice, ISNULL(p.PackSize,1) AS PackSize " +
                        "FROM SaleDetail d INNER JOIN SaleHeader h ON d.SaleID=h.SaleID " +
                        "INNER JOIN Products p ON d.ProductID=p.ProductID " +
                        "WHERE CAST(h.SaleDate AS DATE) BETWEEN @F AND @T ORDER BY h.SaleDate DESC", conn))
                    {
                        cmd.Parameters.AddWithValue("@F", from.Date);
                        cmd.Parameters.AddWithValue("@T", to.Date);
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                            {
                                int qty = Convert.ToInt32(r["Quantity"]);
                                decimal amount = Convert.ToDecimal(r["Amount"]);
                                decimal unitCost = PackMath.UnitCost(
                                    Convert.ToDecimal(r["PurchasePrice"]),
                                    Convert.ToDecimal(r["PackSize"]));
                                decimal cogs = Math.Round(unitCost * qty, 2);
                                list.Add(new ProfitRow
                                {
                                    SaleDate = (DateTime)r["SaleDate"],
                                    InvoiceNo = r["InvoiceNo"].ToString(),
                                    ProductCode = r["ProductCode"].ToString(),
                                    ProductName = r["ProductName"].ToString(),
                                    Qty = qty,
                                    SaleAmount = amount,
                                    UnitCost = unitCost,
                                    Cogs = cogs,
                                    GrossProfit = Math.Round(amount - cogs, 2)
                                });
                            }
                    }
                }
            }
            return list;
        }

        private static ProfitRow BuildProfitRow(SqlDataReader r)
        {
            int qty = Convert.ToInt32(r["Quantity"]);
            decimal amount = Convert.ToDecimal(r["Amount"]);
            decimal packPrice = Convert.ToDecimal(r["PurchasePrice"]);
            decimal packSize = Convert.ToDecimal(r["PackSize"]);

            decimal unitCost = 0;
            object avg = r["AvgUnitCost"];
            object last = r["LastUnitCost"];
            if (avg != null && avg != DBNull.Value && Convert.ToDecimal(avg) > 0)
                unitCost = Convert.ToDecimal(avg);
            else if (last != null && last != DBNull.Value && Convert.ToDecimal(last) > 0)
                unitCost = Convert.ToDecimal(last);
            else
                unitCost = PackMath.UnitCost(packPrice, packSize);

            decimal cogs = Math.Round(unitCost * qty, 2);
            return new ProfitRow
            {
                SaleDate = (DateTime)r["SaleDate"],
                InvoiceNo = r["InvoiceNo"].ToString(),
                ProductCode = r["ProductCode"].ToString(),
                ProductName = r["ProductName"].ToString(),
                Qty = qty,
                SaleAmount = amount,
                UnitCost = Math.Round(unitCost, 4),
                Cogs = cogs,
                GrossProfit = Math.Round(amount - cogs, 2)
            };
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
