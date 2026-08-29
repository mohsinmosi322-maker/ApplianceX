using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Data
{
    public class SaleRepository
    {
        private readonly InventoryService _inv = new InventoryService();
        private readonly CustomerAccountService _acct = new CustomerAccountService();

        public int SaveSale(SaleHeader sale)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string prefix = GetSetting(conn, trans, "InvoicePrefix", "INV-");
                        int num = NextCounter(conn, trans, "NextInvoiceNumber");
                        string invoiceNo = prefix + num.ToString("D6");
                        sale.InvoiceNo = invoiceNo;

                        int saleId;
                        using (var cmd = DbHelper.CreateCommand(
                            "INSERT INTO SaleHeader(InvoiceNo,SaleDate,CustomerID,TotalAmount,Discount,NetAmount,PaidAmount,BalanceAmount,Remarks) " +
                            "VALUES(@Inv,@Dt,@Cust,@Tot,@Disc,@Net,@Paid,@Bal,@Rem); SELECT SCOPE_IDENTITY();", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Inv", invoiceNo);
                            cmd.Parameters.AddWithValue("@Dt", sale.SaleDate);
                            cmd.Parameters.AddWithValue("@Cust", sale.CustomerID);
                            cmd.Parameters.AddWithValue("@Tot", sale.TotalAmount);
                            cmd.Parameters.AddWithValue("@Disc", sale.Discount);
                            cmd.Parameters.AddWithValue("@Net", sale.NetAmount);
                            cmd.Parameters.AddWithValue("@Paid", sale.PaidAmount);
                            cmd.Parameters.AddWithValue("@Bal", sale.BalanceAmount);
                            cmd.Parameters.AddWithValue("@Rem", (object)sale.Remarks ?? DBNull.Value);
                            saleId = Convert.ToInt32(cmd.ExecuteScalar());
                            sale.SaleID = saleId;
                        }

                        foreach (var d in sale.Details)
                        {
                            decimal unitCost = ReadUnitCost(conn, trans, d.ProductID);
                            _inv.EnsureStock(conn, trans, d.ProductID, d.Quantity, d.ProductName);
                            InsertSaleDetail(conn, trans, saleId, d, unitCost);

                            _inv.Post(
                                conn, trans,
                                d.ProductID,
                                InventoryTransactionType.Sale,
                                saleId,
                                quantityIn: 0,
                                quantityOut: d.Quantity,
                                unitCost: unitCost,
                                remarks: "Sale: " + invoiceNo,
                                when: sale.SaleDate);
                        }

                        // Customer ledger: full net as receivable, then payment credit if any
                        _acct.PostSale(conn, trans, sale.CustomerID, saleId, invoiceNo, sale.NetAmount);
                        if (sale.PaidAmount > 0)
                        {
                            try
                            {
                                using (var cmd = DbHelper.CreateCommand(
                                    "INSERT INTO CustomerLedger(CustomerID,EntryDate,EntryType,ReferenceID,ReferenceNo,Debit,Credit,Remarks,CreatedBy) " +
                                    "VALUES(@C,@Dt,'PAYMENT',@R,@No,0,@A,'Sale payment',@By)", conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@C", sale.CustomerID);
                                    cmd.Parameters.AddWithValue("@Dt", sale.SaleDate);
                                    cmd.Parameters.AddWithValue("@R", saleId);
                                    cmd.Parameters.AddWithValue("@No", invoiceNo);
                                    cmd.Parameters.AddWithValue("@A", sale.PaidAmount);
                                    cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch (SqlException) { }
                        }

                        trans.Commit();
                        AppLog.Info("Sale saved " + invoiceNo + " id=" + saleId);
                        return saleId;
                    }
                    catch (Exception ex)
                    {
                        try { trans.Rollback(); } catch { }
                        AppLog.Error("SaveSale failed", ex);
                        throw;
                    }
                }
            }
        }

        private static decimal ReadUnitCost(SqlConnection conn, SqlTransaction trans, int productId)
        {
            using (var cmd = DbHelper.CreateCommand(
                "SELECT PurchasePrice FROM Products WITH (UPDLOCK, ROWLOCK) WHERE ProductID=@P AND IsActive=1", conn, trans))
            {
                cmd.Parameters.AddWithValue("@P", productId);
                var o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value)
                    throw new InvalidOperationException("Product not found or inactive (ID " + productId + ").");
                return Convert.ToDecimal(o);
            }
        }

        public void SaveSaleReturn(int productId, int qty, string reason)
        {
            if (qty <= 0) throw new InvalidOperationException("Return quantity must be > 0.");
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        decimal unitCost = ReadUnitCost(conn, trans, productId);
                        _inv.Post(
                            conn, trans,
                            productId,
                            InventoryTransactionType.SaleReturn,
                            null,
                            quantityIn: qty,
                            quantityOut: 0,
                            unitCost: unitCost,
                            remarks: string.IsNullOrEmpty(reason) ? "Sale return" : reason);

                        trans.Commit();
                        AppLog.Info("Sale return product=" + productId + " qty=" + qty);
                    }
                    catch (Exception ex)
                    {
                        try { trans.Rollback(); } catch { }
                        AppLog.Error("SaveSaleReturn failed", ex);
                        throw;
                    }
                }
            }
        }

        public List<ProductSaleHistoryRow> GetProductSaleHistory(int productId)
        {
            var list = new List<ProductSaleHistoryRow>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT h.SaleDate, h.InvoiceNo, c.CustomerName, d.Quantity, d.SalePrice, d.Amount " +
                    "FROM SaleDetail d INNER JOIN SaleHeader h ON d.SaleID=h.SaleID " +
                    "INNER JOIN Customers c ON h.CustomerID=c.CustomerID " +
                    "WHERE d.ProductID=@P ORDER BY h.SaleDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@P", productId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new ProductSaleHistoryRow
                            {
                                Date = (DateTime)r["SaleDate"],
                                Invoice = r["InvoiceNo"].ToString(),
                                Customer = r["CustomerName"].ToString(),
                                Qty = Convert.ToInt32(r["Quantity"]),
                                Price = Convert.ToDecimal(r["SalePrice"]),
                                Amount = Convert.ToDecimal(r["Amount"])
                            });
                }
            }
            return list;
        }

        private static void InsertSaleDetail(SqlConnection conn, SqlTransaction trans, int saleId, SaleDetail d, decimal unitCost)
        {
            try
            {
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO SaleDetail(SaleID,ProductID,Quantity,SalePrice,Discount,Amount,UnitCost) VALUES(@S,@P,@Q,@Pr,@Di,@Am,@Uc)", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@S", saleId);
                    cmd.Parameters.AddWithValue("@P", d.ProductID);
                    cmd.Parameters.AddWithValue("@Q", d.Quantity);
                    cmd.Parameters.AddWithValue("@Pr", d.SalePrice);
                    cmd.Parameters.AddWithValue("@Di", d.Discount);
                    cmd.Parameters.AddWithValue("@Am", d.Amount);
                    cmd.Parameters.AddWithValue("@Uc", unitCost);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException)
            {
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO SaleDetail(SaleID,ProductID,Quantity,SalePrice,Discount,Amount) VALUES(@S,@P,@Q,@Pr,@Di,@Am)", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@S", saleId);
                    cmd.Parameters.AddWithValue("@P", d.ProductID);
                    cmd.Parameters.AddWithValue("@Q", d.Quantity);
                    cmd.Parameters.AddWithValue("@Pr", d.SalePrice);
                    cmd.Parameters.AddWithValue("@Di", d.Discount);
                    cmd.Parameters.AddWithValue("@Am", d.Amount);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<SaleHeader> GetSales(DateTime from, DateTime to)
        {
            var list = new List<SaleHeader>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT s.*,c.CustomerName FROM SaleHeader s INNER JOIN Customers c ON s.CustomerID=c.CustomerID " +
                    "WHERE CAST(s.SaleDate AS DATE) BETWEEN @F AND @T ORDER BY s.SaleDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@F", from.Date);
                    cmd.Parameters.AddWithValue("@T", to.Date);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new SaleHeader
                            {
                                SaleID = (int)r["SaleID"],
                                InvoiceNo = r["InvoiceNo"].ToString(),
                                SaleDate = (DateTime)r["SaleDate"],
                                CustomerID = (int)r["CustomerID"],
                                CustomerName = r["CustomerName"].ToString(),
                                TotalAmount = (decimal)r["TotalAmount"],
                                Discount = (decimal)r["Discount"],
                                NetAmount = (decimal)r["NetAmount"],
                                PaidAmount = (decimal)r["PaidAmount"],
                                BalanceAmount = (decimal)r["BalanceAmount"]
                            });
                }
            }
            return list;
        }

        public void UpdateHeader(int id, decimal paid, decimal balance, string remarks)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "UPDATE SaleHeader SET PaidAmount=@P, BalanceAmount=@B, Remarks=@R WHERE SaleID=@ID", conn))
                {
                    cmd.Parameters.AddWithValue("@P", paid);
                    cmd.Parameters.AddWithValue("@B", balance);
                    cmd.Parameters.AddWithValue("@R", (object)remarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }
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
