using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Data
{
    public class SaleRepository
    {
        /// <summary>
        /// Saves sale under a single transaction:
        /// locked invoice number, stock check + decrement, ledger OUT, UnitCost snapshot.
        /// Populates sale.SaleID and sale.InvoiceNo on success.
        /// </summary>
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
                            // Lock product row and ensure stock
                            int available;
                            decimal unitCost;
                            using (var cmd = DbHelper.CreateCommand(
                                "SELECT CurrentStock, PurchasePrice FROM Products WITH (UPDLOCK, ROWLOCK) WHERE ProductID=@P AND IsActive=1", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@P", d.ProductID);
                                using (var r = cmd.ExecuteReader())
                                {
                                    if (!r.Read())
                                        throw new InvalidOperationException("Product not found or inactive (ID " + d.ProductID + ").");
                                    available = Convert.ToInt32(r["CurrentStock"]);
                                    unitCost = Convert.ToDecimal(r["PurchasePrice"]);
                                }
                            }

                            if (available < d.Quantity)
                                throw new InvalidOperationException(
                                    "Insufficient stock for " + (d.ProductName ?? ("Product " + d.ProductID)) +
                                    ". Available: " + available + ", requested: " + d.Quantity);

                            // Prefer UnitCost column if migration applied; otherwise omit
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

                            using (var cmd = DbHelper.CreateCommand(
                                "UPDATE Products SET CurrentStock = CurrentStock - @Q WHERE ProductID=@P AND CurrentStock >= @Q", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Q", d.Quantity);
                                cmd.Parameters.AddWithValue("@P", d.ProductID);
                                if (cmd.ExecuteNonQuery() == 0)
                                    throw new InvalidOperationException("Stock changed concurrently for product " + d.ProductID);
                            }

                            using (var cmd = DbHelper.CreateCommand(
                                "INSERT INTO InventoryTransaction(TransactionDate,ProductID,TransactionType,ReferenceID,QuantityIn,QuantityOut,UnitCost,Remarks) " +
                                "VALUES(@Dt,@P,@T,@R,0,@Q,@C,@Rem)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Dt", sale.SaleDate);
                                cmd.Parameters.AddWithValue("@P", d.ProductID);
                                cmd.Parameters.AddWithValue("@T", InventoryTransactionType.Sale);
                                cmd.Parameters.AddWithValue("@R", saleId);
                                cmd.Parameters.AddWithValue("@Q", d.Quantity);
                                cmd.Parameters.AddWithValue("@C", unitCost);
                                cmd.Parameters.AddWithValue("@Rem", "Sale: " + invoiceNo);
                                cmd.ExecuteNonQuery();
                            }
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

        /// <summary>Atomically allocates next counter under row lock.</summary>
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
