using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Data
{
    public class SaleRepository
    {
        public int SaveSale(SaleHeader sale)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string prefix = AppSettings.Get("InvoicePrefix"); if (string.IsNullOrEmpty(prefix)) prefix = "INV-";
                        string nextNo = AppSettings.Get("NextInvoiceNumber"); if (string.IsNullOrEmpty(nextNo)) nextNo = "1";
                        int num = int.Parse(nextNo);
                        string invoiceNo = prefix + num.ToString("D6");
                        int saleId;
                        using (var cmd = DbHelper.CreateCommand(
                            "INSERT INTO SaleHeader(InvoiceNo,SaleDate,CustomerID,TotalAmount,Discount,NetAmount,PaidAmount,BalanceAmount,Remarks) VALUES(@Inv,@Dt,@Cust,@Tot,@Disc,@Net,@Paid,@Bal,@Rem); SELECT SCOPE_IDENTITY();", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Inv", invoiceNo); cmd.Parameters.AddWithValue("@Dt", sale.SaleDate);
                            cmd.Parameters.AddWithValue("@Cust", sale.CustomerID); cmd.Parameters.AddWithValue("@Tot", sale.TotalAmount);
                            cmd.Parameters.AddWithValue("@Disc", sale.Discount); cmd.Parameters.AddWithValue("@Net", sale.NetAmount);
                            cmd.Parameters.AddWithValue("@Paid", sale.PaidAmount); cmd.Parameters.AddWithValue("@Bal", sale.BalanceAmount);
                            cmd.Parameters.AddWithValue("@Rem", (object)sale.Remarks ?? DBNull.Value);
                            saleId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        foreach (var d in sale.Details)
                        {
                            using (var cmd = DbHelper.CreateCommand("INSERT INTO SaleDetail(SaleID,ProductID,Quantity,SalePrice,Discount,Amount) VALUES(@S,@P,@Q,@Pr,@Di,@Am)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@S", saleId); cmd.Parameters.AddWithValue("@P", d.ProductID);
                                cmd.Parameters.AddWithValue("@Q", d.Quantity); cmd.Parameters.AddWithValue("@Pr", d.SalePrice);
                                cmd.Parameters.AddWithValue("@Di", d.Discount); cmd.Parameters.AddWithValue("@Am", d.Amount);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = DbHelper.CreateCommand("INSERT INTO InventoryTransaction(TransactionDate,ProductID,TransactionType,ReferenceID,QuantityIn,QuantityOut,UnitCost,Remarks) VALUES(@Dt,@P,@T,@R,0,@Q,@C,@Rem)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Dt", sale.SaleDate); cmd.Parameters.AddWithValue("@P", d.ProductID);
                                cmd.Parameters.AddWithValue("@T", InventoryTransactionType.Sale); cmd.Parameters.AddWithValue("@R", saleId);
                                cmd.Parameters.AddWithValue("@Q", d.Quantity); cmd.Parameters.AddWithValue("@C", d.SalePrice);
                                cmd.Parameters.AddWithValue("@Rem", "Sale: " + invoiceNo); cmd.ExecuteNonQuery();
                            }
                        }
                        AppSettings.Set("NextInvoiceNumber", (num + 1).ToString());
                        trans.Commit();
                        return saleId;
                    }
                    catch { trans.Rollback(); throw; }
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
                    "SELECT s.*,c.CustomerName FROM SaleHeader s INNER JOIN Customers c ON s.CustomerID=c.CustomerID WHERE CAST(s.SaleDate AS DATE) BETWEEN @F AND @T ORDER BY s.SaleDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@F", from.Date); cmd.Parameters.AddWithValue("@T", to.Date);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new SaleHeader {
                                SaleID=(int)r["SaleID"], InvoiceNo=r["InvoiceNo"].ToString(), SaleDate=(DateTime)r["SaleDate"],
                                CustomerID=(int)r["CustomerID"], CustomerName=r["CustomerName"].ToString(),
                                TotalAmount=(decimal)r["TotalAmount"], Discount=(decimal)r["Discount"],
                                NetAmount=(decimal)r["NetAmount"], PaidAmount=(decimal)r["PaidAmount"], BalanceAmount=(decimal)r["BalanceAmount"]
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
                using (var cmd = DbHelper.CreateCommand("UPDATE SaleHeader SET PaidAmount=@P, BalanceAmount=@B, Remarks=@R WHERE SaleID=@ID", conn))
                {
                    cmd.Parameters.AddWithValue("@P", paid);
                    cmd.Parameters.AddWithValue("@B", balance);
                    cmd.Parameters.AddWithValue("@R", (object)remarks ?? System.DBNull.Value);
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}
