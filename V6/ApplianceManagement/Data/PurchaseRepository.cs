using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;
using ApplianceManagement.Services;

namespace ApplianceManagement.Data
{
    public class PurchaseRepository
    {
        private readonly InventoryService _inv = new InventoryService();
        private readonly SupplierAccountService _acct = new SupplierAccountService();

        public int SavePurchase(PurchaseHeader purchase)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string prefix = InvoiceNumberHelper.ResolvePurchasePrefix(conn, trans);
                        int num = InvoiceNumberHelper.NextCounter(conn, trans, "NextPurchaseInvoiceNumber");
                        string invoiceNo = InvoiceNumberHelper.Format(prefix, num);
                        purchase.InvoiceNo = invoiceNo;

                        int purchaseId;
                        using (var cmd = DbHelper.CreateCommand(
                            "INSERT INTO PurchaseHeader(InvoiceNo,PurchaseDate,SupplierID,TotalAmount,Discount,NetAmount,PaidAmount,BalanceAmount,Remarks) " +
                            "VALUES(@Inv,@Dt,@Sup,@Tot,@Disc,@Net,@Paid,@Bal,@Rem); SELECT SCOPE_IDENTITY();", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Inv", invoiceNo);
                            cmd.Parameters.AddWithValue("@Dt", purchase.PurchaseDate);
                            cmd.Parameters.AddWithValue("@Sup", purchase.SupplierID);
                            cmd.Parameters.AddWithValue("@Tot", purchase.TotalAmount);
                            cmd.Parameters.AddWithValue("@Disc", purchase.Discount);
                            cmd.Parameters.AddWithValue("@Net", purchase.NetAmount);
                            cmd.Parameters.AddWithValue("@Paid", purchase.PaidAmount);
                            cmd.Parameters.AddWithValue("@Bal", purchase.BalanceAmount);
                            cmd.Parameters.AddWithValue("@Rem", (object)purchase.Remarks ?? DBNull.Value);
                            purchaseId = Convert.ToInt32(cmd.ExecuteScalar());
                            purchase.PurchaseID = purchaseId;
                        }

                        foreach (var d in purchase.Details)
                        {
                            decimal packSize = ReadPackSize(conn, trans, d.ProductID);
                            int packs = d.Quantity < 1 ? 1 : d.Quantity;
                            decimal packPrice = d.PurchasePrice;
                            if (packPrice < 0) packPrice = 0;
                            int unitsIn = PackMath.PacksToUnits(packs, packSize);
                            decimal unitCost = PackMath.UnitCost(packPrice, packSize);

                            using (var cmd = DbHelper.CreateCommand(
                                "INSERT INTO PurchaseDetail(PurchaseID,ProductID,Quantity,PurchasePrice,Discount,Amount) VALUES(@P,@Pr,@Q,@Price,@Di,@Am)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@P", purchaseId);
                                cmd.Parameters.AddWithValue("@Pr", d.ProductID);
                                cmd.Parameters.AddWithValue("@Q", packs);
                                cmd.Parameters.AddWithValue("@Price", packPrice);
                                cmd.Parameters.AddWithValue("@Di", d.Discount);
                                cmd.Parameters.AddWithValue("@Am", d.Amount);
                                cmd.ExecuteNonQuery();
                            }

                            try
                            {
                                using (var cmd = DbHelper.CreateCommand(
                                    "UPDATE Products SET PurchasePrice = @PackPrice WHERE ProductID=@P", conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@PackPrice", packPrice);
                                    cmd.Parameters.AddWithValue("@P", d.ProductID);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch (SqlException) { }

                            _inv.Post(conn, trans, d.ProductID, InventoryTransactionType.Purchase, purchaseId,
                                unitsIn, 0, unitCost,
                                "Purchase: " + invoiceNo + " packs=" + packs + " packSize=" + packSize,
                                purchase.PurchaseDate);
                        }

                        _acct.PostPurchase(conn, trans, purchase.SupplierID, purchaseId, invoiceNo, purchase.NetAmount);
                        if (purchase.PaidAmount > 0)
                        {
                            try
                            {
                                using (var cmd = DbHelper.CreateCommand(
                                    "INSERT INTO SupplierLedger(SupplierID,EntryDate,EntryType,ReferenceID,ReferenceNo,Debit,Credit,Remarks,CreatedBy) " +
                                    "VALUES(@S,@Dt,'PAYMENT',@R,@No,@A,0,'Purchase payment',@By)", conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@S", purchase.SupplierID);
                                    cmd.Parameters.AddWithValue("@Dt", purchase.PurchaseDate);
                                    cmd.Parameters.AddWithValue("@R", purchaseId);
                                    cmd.Parameters.AddWithValue("@No", invoiceNo);
                                    cmd.Parameters.AddWithValue("@A", purchase.PaidAmount);
                                    cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch (SqlException) { }
                        }

                        trans.Commit();
                        AppLog.Info("Purchase saved " + invoiceNo + " id=" + purchaseId);
                        return purchaseId;
                    }
                    catch (Exception ex)
                    {
                        try { trans.Rollback(); } catch { }
                        AppLog.Error("SavePurchase failed", ex);
                        throw;
                    }
                }
            }
        }

        private static decimal ReadPackSize(SqlConnection conn, SqlTransaction trans, int productId)
        {
            try
            {
                using (var cmd = DbHelper.CreateCommand("SELECT ISNULL(PackSize,1) FROM Products WHERE ProductID=@P", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@P", productId);
                    var o = cmd.ExecuteScalar();
                    if (o == null || o == DBNull.Value) return 1m;
                    return PackMath.NormalizePackSize(Convert.ToDecimal(o));
                }
            }
            catch (SqlException) { return 1m; }
        }

        public List<ProductPurchaseHistoryRow> GetProductPurchaseHistory(int productId)
        {
            var list = new List<ProductPurchaseHistoryRow>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT h.PurchaseDate, h.InvoiceNo, s.SupplierName, d.Quantity, d.PurchasePrice, d.Amount " +
                    "FROM PurchaseDetail d INNER JOIN PurchaseHeader h ON d.PurchaseID=h.PurchaseID " +
                    "INNER JOIN Suppliers s ON h.SupplierID=s.SupplierID WHERE d.ProductID=@P ORDER BY h.PurchaseDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@P", productId);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new ProductPurchaseHistoryRow
                            {
                                Date = (DateTime)r["PurchaseDate"],
                                Invoice = r["InvoiceNo"].ToString(),
                                Supplier = r["SupplierName"].ToString(),
                                Qty = Convert.ToInt32(r["Quantity"]),
                                Price = Convert.ToDecimal(r["PurchasePrice"]),
                                Amount = Convert.ToDecimal(r["Amount"])
                            });
                }
            }
            return list;
        }

        public List<PurchaseHeader> GetPurchases(DateTime from, DateTime to)
        {
            var list = new List<PurchaseHeader>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT p.*,s.SupplierName FROM PurchaseHeader p INNER JOIN Suppliers s ON p.SupplierID=s.SupplierID " +
                    "WHERE CAST(p.PurchaseDate AS DATE) BETWEEN @F AND @T ORDER BY p.PurchaseDate DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@F", from.Date);
                    cmd.Parameters.AddWithValue("@T", to.Date);
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new PurchaseHeader
                            {
                                PurchaseID = (int)r["PurchaseID"],
                                InvoiceNo = r["InvoiceNo"].ToString(),
                                PurchaseDate = (DateTime)r["PurchaseDate"],
                                SupplierID = (int)r["SupplierID"],
                                SupplierName = r["SupplierName"].ToString(),
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
                    "UPDATE PurchaseHeader SET PaidAmount=@P, BalanceAmount=@B, Remarks=@R WHERE PurchaseID=@ID", conn))
                {
                    cmd.Parameters.AddWithValue("@P", paid);
                    cmd.Parameters.AddWithValue("@B", balance);
                    cmd.Parameters.AddWithValue("@R", (object)remarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
