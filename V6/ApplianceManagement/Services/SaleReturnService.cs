using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Services
{
    /// <summary>
    /// Invoice-linked sale returns. Prevents over-return; posts inventory via InventoryService.
    /// </summary>
    public class SaleReturnService
    {
        private readonly InventoryService _inv = new InventoryService();

        public List<SaleInvoiceLine> LoadInvoice(string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                throw new InvalidOperationException("Enter original sale invoice number.");

            var list = new List<SaleInvoiceLine>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT d.SaleDetailID, h.SaleID, h.InvoiceNo, h.SaleDate, h.CustomerID, c.CustomerName, " +
                    "d.ProductID, p.ProductCode, p.ProductName, d.Quantity AS SoldQty, d.SalePrice, d.Amount, " +
                    "ISNULL((SELECT SUM(rd.Quantity) FROM SaleReturnDetail rd " +
                    "  INNER JOIN SaleReturnHeader rh ON rd.SaleReturnID = rh.SaleReturnID " +
                    "  WHERE rd.OriginalSaleDetailID = d.SaleDetailID), 0) AS AlreadyReturned " +
                    "FROM SaleDetail d " +
                    "INNER JOIN SaleHeader h ON d.SaleID = h.SaleID " +
                    "INNER JOIN Customers c ON h.CustomerID = c.CustomerID " +
                    "INNER JOIN Products p ON d.ProductID = p.ProductID " +
                    "WHERE h.InvoiceNo = @Inv " +
                    "ORDER BY d.SaleDetailID", conn))
                {
                    cmd.Parameters.AddWithValue("@Inv", invoiceNo.Trim());
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new SaleInvoiceLine
                            {
                                SaleDetailID = (int)r["SaleDetailID"],
                                SaleID = (int)r["SaleID"],
                                InvoiceNo = r["InvoiceNo"].ToString(),
                                SaleDate = (DateTime)r["SaleDate"],
                                CustomerID = (int)r["CustomerID"],
                                CustomerName = r["CustomerName"].ToString(),
                                ProductID = (int)r["ProductID"],
                                ProductCode = r["ProductCode"].ToString(),
                                ProductName = r["ProductName"].ToString(),
                                SoldQty = Convert.ToInt32(r["SoldQty"]),
                                AlreadyReturned = Convert.ToInt32(r["AlreadyReturned"]),
                                SalePrice = Convert.ToDecimal(r["SalePrice"]),
                                Amount = Convert.ToDecimal(r["Amount"])
                            });
                        }
                    }
                }
            }

            if (list.Count == 0)
                throw new InvalidOperationException("Invoice not found: " + invoiceNo.Trim());

            return list;
        }

        public void Validate(SaleReturnHeader ret)
        {
            if (ret == null) throw new ArgumentNullException("ret");
            if (ret.OriginalSaleID <= 0)
                throw new InvalidOperationException("Load an original sale invoice first.");
            if (ret.Details == null || ret.Details.Count == 0)
                throw new InvalidOperationException("Select at least one line to return.");
            if (string.IsNullOrWhiteSpace(ret.Remarks))
                throw new InvalidOperationException("Return reason is required.");

            decimal gross = 0;
            foreach (var d in ret.Details)
            {
                if (d.Quantity <= 0)
                    throw new InvalidOperationException("Return qty must be > 0 for " + (d.ProductName ?? "product") + ".");
                if (d.SoldQty > 0 && d.Quantity > d.ReturnableQty)
                    throw new InvalidOperationException(
                        "Cannot return more than available for " + (d.ProductName ?? d.ProductCode) +
                        ". Returnable: " + d.ReturnableQty + ", requested: " + d.Quantity);
                d.Amount = Math.Round(d.Quantity * d.SalePrice, 2);
                gross += d.Amount;
            }

            var totals = TransactionTotals.Calculate(gross, ret.Discount, ret.RefundAmount);
            ret.TotalAmount = totals.Gross;
            ret.Discount = totals.DiscountAmount;
            ret.NetAmount = totals.Net;
            if (ret.RefundAmount <= 0) ret.RefundAmount = totals.Net;
            if (ret.ReturnDate == default(DateTime)) ret.ReturnDate = DateTime.Now;
        }

        public int Save(SaleReturnHeader ret)
        {
            Validate(ret);

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // Re-check returnable under lock
                        foreach (var d in ret.Details)
                        {
                            if (d.OriginalSaleDetailID.HasValue)
                            {
                                int returnable = GetReturnableLocked(conn, trans, d.OriginalSaleDetailID.Value);
                                if (d.Quantity > returnable)
                                    throw new InvalidOperationException(
                                        "Over-return blocked for " + (d.ProductName ?? "product") +
                                        ". Returnable now: " + returnable);
                            }
                        }

                        string prefix = GetSetting(conn, trans, "SaleReturnPrefix", "RET-");
                        int num = NextCounter(conn, trans, "NextSaleReturnNumber");
                        string returnNo = prefix + num.ToString("D6");
                        ret.ReturnNo = returnNo;

                        int returnId;
                        using (var cmd = DbHelper.CreateCommand(
                            "INSERT INTO SaleReturnHeader(ReturnNo,ReturnDate,OriginalSaleID,CustomerID,TotalAmount,Discount,NetAmount,RefundAmount,Remarks) " +
                            "VALUES(@No,@Dt,@Sale,@Cust,@Tot,@Disc,@Net,@Ref,@Rem); SELECT SCOPE_IDENTITY();", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@No", returnNo);
                            cmd.Parameters.AddWithValue("@Dt", ret.ReturnDate);
                            cmd.Parameters.AddWithValue("@Sale", ret.OriginalSaleID);
                            cmd.Parameters.AddWithValue("@Cust", ret.CustomerID);
                            cmd.Parameters.AddWithValue("@Tot", ret.TotalAmount);
                            cmd.Parameters.AddWithValue("@Disc", ret.Discount);
                            cmd.Parameters.AddWithValue("@Net", ret.NetAmount);
                            cmd.Parameters.AddWithValue("@Ref", ret.RefundAmount);
                            cmd.Parameters.AddWithValue("@Rem", (object)ret.Remarks ?? DBNull.Value);
                            returnId = Convert.ToInt32(cmd.ExecuteScalar());
                            ret.SaleReturnID = returnId;
                        }

                        foreach (var d in ret.Details)
                        {
                            using (var cmd = DbHelper.CreateCommand(
                                "INSERT INTO SaleReturnDetail(SaleReturnID,OriginalSaleDetailID,ProductID,Quantity,SalePrice,Amount) " +
                                "VALUES(@R,@Od,@P,@Q,@Pr,@Am)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@R", returnId);
                                cmd.Parameters.AddWithValue("@Od", (object)d.OriginalSaleDetailID ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@P", d.ProductID);
                                cmd.Parameters.AddWithValue("@Q", d.Quantity);
                                cmd.Parameters.AddWithValue("@Pr", d.SalePrice);
                                cmd.Parameters.AddWithValue("@Am", d.Amount);
                                cmd.ExecuteNonQuery();
                            }

                            decimal unitCost = ReadUnitCost(conn, trans, d.ProductID);
                            _inv.Post(
                                conn, trans,
                                d.ProductID,
                                InventoryTransactionType.SaleReturn,
                                returnId,
                                quantityIn: d.Quantity,
                                quantityOut: 0,
                                unitCost: unitCost,
                                remarks: "Sale return " + returnNo + " for invoice " + (ret.OriginalInvoiceNo ?? ""),
                                when: ret.ReturnDate);
                        }

                        trans.Commit();
                        AppLog.Info("Sale return saved " + returnNo + " id=" + returnId);
                        return returnId;
                    }
                    catch (Exception ex)
                    {
                        try { trans.Rollback(); } catch { }
                        AppLog.Error("SaleReturn save failed", ex);
                        throw;
                    }
                }
            }
        }

        private static int GetReturnableLocked(SqlConnection conn, SqlTransaction trans, int saleDetailId)
        {
            using (var cmd = DbHelper.CreateCommand(
                "SELECT d.Quantity - ISNULL((SELECT SUM(rd.Quantity) FROM SaleReturnDetail rd WHERE rd.OriginalSaleDetailID=d.SaleDetailID),0) " +
                "FROM SaleDetail d WITH (UPDLOCK, ROWLOCK) WHERE d.SaleDetailID=@ID", conn, trans))
            {
                cmd.Parameters.AddWithValue("@ID", saleDetailId);
                var o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value) return 0;
                int n = Convert.ToInt32(o);
                return n < 0 ? 0 : n;
            }
        }

        private static decimal ReadUnitCost(SqlConnection conn, SqlTransaction trans, int productId)
        {
            using (var cmd = DbHelper.CreateCommand("SELECT PurchasePrice FROM Products WHERE ProductID=@P", conn, trans))
            {
                cmd.Parameters.AddWithValue("@P", productId);
                var o = cmd.ExecuteScalar();
                return o == null || o == DBNull.Value ? 0m : Convert.ToDecimal(o);
            }
        }

        private static string GetSetting(SqlConnection conn, SqlTransaction trans, string name, string defaultValue)
        {
            using (var cmd = DbHelper.CreateCommand("SELECT SettingValue FROM Settings WHERE SettingName=@N", conn, trans))
            {
                cmd.Parameters.AddWithValue("@N", name);
                var r = cmd.ExecuteScalar();
                if (r == null || r == DBNull.Value || string.IsNullOrWhiteSpace(r.ToString()))
                    return defaultValue;
                return r.ToString();
            }
        }

        private static int NextCounter(SqlConnection conn, SqlTransaction trans, string settingName)
        {
            using (var cmd = DbHelper.CreateCommand(
                "IF NOT EXISTS (SELECT 1 FROM Settings WITH (UPDLOCK, HOLDLOCK) WHERE SettingName=@N) " +
                "INSERT INTO Settings(SettingName,SettingValue) VALUES(@N,'1'); " +
                "UPDATE Settings WITH (UPDLOCK, ROWLOCK) SET SettingValue = CAST(CAST(ISNULL(NULLIF(SettingValue,''),'0') AS INT) + 1 AS NVARCHAR(50)) " +
                "WHERE SettingName=@N; " +
                "SELECT CAST(SettingValue AS INT) - 1 FROM Settings WHERE SettingName=@N;", conn, trans))
            {
                cmd.Parameters.AddWithValue("@N", settingName);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
