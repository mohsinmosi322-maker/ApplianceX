using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Data
{
    public class PurchaseRepository
    {
        public int SavePurchase(PurchaseHeader purchase)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string prefix = AppSettings.Get("PurchaseInvoicePrefix"); if (string.IsNullOrEmpty(prefix)) prefix = "PUR-";
                        string nextNo = AppSettings.Get("NextPurchaseInvoiceNumber"); if (string.IsNullOrEmpty(nextNo)) nextNo = "1";
                        int num = int.Parse(nextNo);
                        string invoiceNo = prefix + num.ToString("D6");
                        int purchaseId;
                        using (var cmd = DbHelper.CreateCommand(
                            "INSERT INTO PurchaseHeader(InvoiceNo,PurchaseDate,SupplierID,TotalAmount,Discount,NetAmount,PaidAmount,BalanceAmount,Remarks) VALUES(@Inv,@Dt,@Sup,@Tot,@Disc,@Net,@Paid,@Bal,@Rem); SELECT SCOPE_IDENTITY();", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Inv", invoiceNo); cmd.Parameters.AddWithValue("@Dt", purchase.PurchaseDate);
                            cmd.Parameters.AddWithValue("@Sup", purchase.SupplierID); cmd.Parameters.AddWithValue("@Tot", purchase.TotalAmount);
                            cmd.Parameters.AddWithValue("@Disc", purchase.Discount); cmd.Parameters.AddWithValue("@Net", purchase.NetAmount);
                            cmd.Parameters.AddWithValue("@Paid", purchase.PaidAmount); cmd.Parameters.AddWithValue("@Bal", purchase.BalanceAmount);
                            cmd.Parameters.AddWithValue("@Rem", (object)purchase.Remarks ?? DBNull.Value);
                            purchaseId = Convert.ToInt32(cmd.ExecuteScalar());
                        }
                        foreach (var d in purchase.Details)
                        {
                            using (var cmd = DbHelper.CreateCommand("INSERT INTO PurchaseDetail(PurchaseID,ProductID,Quantity,PurchasePrice,Discount,Amount) VALUES(@P,@Pr,@Q,@Price,@Di,@Am)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@P", purchaseId); cmd.Parameters.AddWithValue("@Pr", d.ProductID);
                                cmd.Parameters.AddWithValue("@Q", d.Quantity); cmd.Parameters.AddWithValue("@Price", d.PurchasePrice);
                                cmd.Parameters.AddWithValue("@Di", d.Discount); cmd.Parameters.AddWithValue("@Am", d.Amount);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = DbHelper.CreateCommand("INSERT INTO InventoryTransaction(TransactionDate,ProductID,TransactionType,ReferenceID,QuantityIn,QuantityOut,UnitCost,Remarks) VALUES(@Dt,@P,@T,@R,@Q,0,@C,@Rem)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Dt", purchase.PurchaseDate); cmd.Parameters.AddWithValue("@P", d.ProductID);
                                cmd.Parameters.AddWithValue("@T", InventoryTransactionType.Purchase); cmd.Parameters.AddWithValue("@R", purchaseId);
                                cmd.Parameters.AddWithValue("@Q", d.Quantity); cmd.Parameters.AddWithValue("@C", d.PurchasePrice);
                                cmd.Parameters.AddWithValue("@Rem", "Purchase: " + invoiceNo); cmd.ExecuteNonQuery();
                            }
                        }
                        AppSettings.Set("NextPurchaseInvoiceNumber", (num + 1).ToString());
                        trans.Commit();
                        return purchaseId;
                    }
                    catch { trans.Rollback(); throw; }
                }
            }
        }
        public List<PurchaseHeader> GetPurchases(DateTime from, DateTime to)
        {
            var list = new List<PurchaseHeader>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT p.*,s.SupplierName FROM PurchaseHeader p INNER JOIN Suppliers s ON p.SupplierID=s.SupplierID WHERE CAST(p.PurchaseDate AS DATE) BETWEEN @F AND @T ORDER BY p.PurchaseDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@F", from.Date); cmd.Parameters.AddWithValue("@T", to.Date);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new PurchaseHeader {
                                PurchaseID=(int)r["PurchaseID"], InvoiceNo=r["InvoiceNo"].ToString(), PurchaseDate=(DateTime)r["PurchaseDate"],
                                SupplierID=(int)r["SupplierID"], SupplierName=r["SupplierName"].ToString(),
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
                using (var cmd = DbHelper.CreateCommand("UPDATE PurchaseHeader SET PaidAmount=@P, BalanceAmount=@B, Remarks=@R WHERE PurchaseID=@ID", conn))
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
