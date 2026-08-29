using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Services
{
    public class PurchaseInvoiceLine
    {
        public int PurchaseDetailID { get; set; }
        public int PurchaseID { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime PurchaseDate { get; set; }
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int PurchasedQty { get; set; } // packs
        public int AlreadyReturned { get; set; }
        public int ReturnableQty { get { return Math.Max(0, PurchasedQty - AlreadyReturned); } }
        public decimal PurchasePrice { get; set; }
        public decimal PackSize { get; set; }
    }

    public class PurchaseReturnHeader
    {
        public int PurchaseReturnID { get; set; }
        public string ReturnNo { get; set; }
        public DateTime ReturnDate { get; set; }
        public int OriginalPurchaseID { get; set; }
        public string OriginalInvoiceNo { get; set; }
        public int SupplierID { get; set; }
        public string SupplierName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string Remarks { get; set; }
        public List<PurchaseReturnDetail> Details { get; set; } = new List<PurchaseReturnDetail>();
    }

    public class PurchaseReturnDetail
    {
        public int? OriginalPurchaseDetailID { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; } // packs returned
        public decimal PurchasePrice { get; set; }
        public decimal Amount { get; set; }
        public int PurchasedQty { get; set; }
        public int AlreadyReturned { get; set; }
        public int ReturnableQty { get { return Math.Max(0, PurchasedQty - AlreadyReturned); } }
        public decimal PackSize { get; set; }
    }

    public class PurchaseReturnService
    {
        private readonly InventoryService _inv = new InventoryService();

        public List<PurchaseInvoiceLine> LoadInvoice(string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
                throw new InvalidOperationException("Enter original purchase invoice number.");
            var list = new List<PurchaseInvoiceLine>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT d.PurchaseDetailID, h.PurchaseID, h.InvoiceNo, h.PurchaseDate, h.SupplierID, s.SupplierName, " +
                    "d.ProductID, p.ProductCode, p.ProductName, d.Quantity AS PurchasedQty, d.PurchasePrice, " +
                    "ISNULL(p.PackSize,1) AS PackSize, " +
                    "ISNULL((SELECT SUM(rd.Quantity) FROM PurchaseReturnDetail rd WHERE rd.OriginalPurchaseDetailID=d.PurchaseDetailID),0) AS AlreadyReturned " +
                    "FROM PurchaseDetail d " +
                    "INNER JOIN PurchaseHeader h ON d.PurchaseID=h.PurchaseID " +
                    "INNER JOIN Suppliers s ON h.SupplierID=s.SupplierID " +
                    "INNER JOIN Products p ON d.ProductID=p.ProductID " +
                    "WHERE h.InvoiceNo=@Inv ORDER BY d.PurchaseDetailID", conn))
                {
                    cmd.Parameters.AddWithValue("@Inv", invoiceNo.Trim());
                    using (var r = cmd.ExecuteReader())
                        while (r.Read())
                            list.Add(new PurchaseInvoiceLine
                            {
                                PurchaseDetailID = (int)r["PurchaseDetailID"],
                                PurchaseID = (int)r["PurchaseID"],
                                InvoiceNo = r["InvoiceNo"].ToString(),
                                PurchaseDate = (DateTime)r["PurchaseDate"],
                                SupplierID = (int)r["SupplierID"],
                                SupplierName = r["SupplierName"].ToString(),
                                ProductID = (int)r["ProductID"],
                                ProductCode = r["ProductCode"].ToString(),
                                ProductName = r["ProductName"].ToString(),
                                PurchasedQty = Convert.ToInt32(r["PurchasedQty"]),
                                AlreadyReturned = Convert.ToInt32(r["AlreadyReturned"]),
                                PurchasePrice = Convert.ToDecimal(r["PurchasePrice"]),
                                PackSize = Convert.ToDecimal(r["PackSize"])
                            });
                }
            }
            if (list.Count == 0) throw new InvalidOperationException("Purchase invoice not found: " + invoiceNo);
            return list;
        }

        public int Save(PurchaseReturnHeader ret)
        {
            if (ret == null) throw new ArgumentNullException("ret");
            if (ret.OriginalPurchaseID <= 0) throw new InvalidOperationException("Load original purchase first.");
            if (ret.Details == null || ret.Details.Count == 0) throw new InvalidOperationException("Select lines to return.");
            if (string.IsNullOrWhiteSpace(ret.Remarks)) throw new InvalidOperationException("Return reason required.");

            decimal gross = 0;
            foreach (var d in ret.Details)
            {
                if (d.Quantity <= 0) throw new InvalidOperationException("Return qty must be > 0.");
                if (d.Quantity > d.ReturnableQty)
                    throw new InvalidOperationException("Over-return for " + d.ProductName + ". Returnable: " + d.ReturnableQty);
                d.Amount = Math.Round(d.Quantity * d.PurchasePrice, 2);
                gross += d.Amount;
            }
            var totals = TransactionTotals.Calculate(gross, ret.Discount, ret.RefundAmount);
            ret.TotalAmount = totals.Gross;
            ret.Discount = totals.DiscountAmount;
            ret.NetAmount = totals.Net;
            if (ret.RefundAmount <= 0) ret.RefundAmount = totals.Net;
            if (ret.ReturnDate == default(DateTime)) ret.ReturnDate = DateTime.Now;

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        string prefix = GetSetting(conn, trans, "PurchaseReturnPrefix", "PR-");
                        int num = NextCounter(conn, trans, "NextPurchaseReturnNumber");
                        string returnNo = prefix + num.ToString("D6");
                        ret.ReturnNo = returnNo;

                        int id;
                        using (var cmd = DbHelper.CreateCommand(
                            "INSERT INTO PurchaseReturnHeader(ReturnNo,ReturnDate,OriginalPurchaseID,SupplierID,TotalAmount,Discount,NetAmount,RefundAmount,Remarks,CreatedBy) " +
                            "VALUES(@No,@Dt,@Pur,@Sup,@Tot,@Disc,@Net,@Ref,@Rem,@By); SELECT SCOPE_IDENTITY();", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@No", returnNo);
                            cmd.Parameters.AddWithValue("@Dt", ret.ReturnDate);
                            cmd.Parameters.AddWithValue("@Pur", ret.OriginalPurchaseID);
                            cmd.Parameters.AddWithValue("@Sup", ret.SupplierID);
                            cmd.Parameters.AddWithValue("@Tot", ret.TotalAmount);
                            cmd.Parameters.AddWithValue("@Disc", ret.Discount);
                            cmd.Parameters.AddWithValue("@Net", ret.NetAmount);
                            cmd.Parameters.AddWithValue("@Ref", ret.RefundAmount);
                            cmd.Parameters.AddWithValue("@Rem", (object)ret.Remarks ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                            id = Convert.ToInt32(cmd.ExecuteScalar());
                            ret.PurchaseReturnID = id;
                        }

                        foreach (var d in ret.Details)
                        {
                            using (var cmd = DbHelper.CreateCommand(
                                "INSERT INTO PurchaseReturnDetail(PurchaseReturnID,OriginalPurchaseDetailID,ProductID,Quantity,PurchasePrice,Amount) " +
                                "VALUES(@R,@Od,@P,@Q,@Pr,@Am)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@R", id);
                                cmd.Parameters.AddWithValue("@Od", (object)d.OriginalPurchaseDetailID ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@P", d.ProductID);
                                cmd.Parameters.AddWithValue("@Q", d.Quantity);
                                cmd.Parameters.AddWithValue("@Pr", d.PurchasePrice);
                                cmd.Parameters.AddWithValue("@Am", d.Amount);
                                cmd.ExecuteNonQuery();
                            }

                            int unitsOut = PackMath.PacksToUnits(d.Quantity, d.PackSize <= 0 ? 1m : d.PackSize);
                            decimal unitCost = PackMath.UnitCost(d.PurchasePrice, d.PackSize <= 0 ? 1m : d.PackSize);
                            _inv.EnsureStock(conn, trans, d.ProductID, unitsOut, d.ProductName);
                            _inv.Post(conn, trans, d.ProductID, InventoryTransactionType.PurchaseReturn, id,
                                0, unitsOut, unitCost,
                                "Purchase return " + returnNo + " for " + (ret.OriginalInvoiceNo ?? ""),
                                ret.ReturnDate);
                        }

                        // Supplier ledger: debit reduces payable
                        try
                        {
                            using (var cmd = DbHelper.CreateCommand(
                                "INSERT INTO SupplierLedger(SupplierID,EntryDate,EntryType,ReferenceID,ReferenceNo,Debit,Credit,Remarks,CreatedBy) " +
                                "VALUES(@S,@Dt,'PURCHASE_RETURN',@R,@No,@Amt,0,@Rem,@By)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@S", ret.SupplierID);
                                cmd.Parameters.AddWithValue("@Dt", ret.ReturnDate);
                                cmd.Parameters.AddWithValue("@R", id);
                                cmd.Parameters.AddWithValue("@No", returnNo);
                                cmd.Parameters.AddWithValue("@Amt", ret.NetAmount);
                                cmd.Parameters.AddWithValue("@Rem", ret.Remarks);
                                cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        catch (SqlException) { /* table may not exist yet */ }

                        trans.Commit();
                        AppLog.Info("Purchase return " + returnNo);
                        return id;
                    }
                    catch (Exception ex)
                    {
                        try { trans.Rollback(); } catch { }
                        AppLog.Error("PurchaseReturn failed", ex);
                        throw;
                    }
                }
            }
        }

        private static string GetSetting(SqlConnection conn, SqlTransaction trans, string name, string def)
        {
            using (var cmd = DbHelper.CreateCommand("SELECT SettingValue FROM Settings WHERE SettingName=@N", conn, trans))
            {
                cmd.Parameters.AddWithValue("@N", name);
                var r = cmd.ExecuteScalar();
                return r == null || r == DBNull.Value || string.IsNullOrWhiteSpace(r.ToString()) ? def : r.ToString();
            }
        }

        private static int NextCounter(SqlConnection conn, SqlTransaction trans, string name)
        {
            using (var cmd = DbHelper.CreateCommand(
                "IF NOT EXISTS (SELECT 1 FROM Settings WITH (UPDLOCK, HOLDLOCK) WHERE SettingName=@N) INSERT INTO Settings(SettingName,SettingValue) VALUES(@N,'1'); " +
                "UPDATE Settings WITH (UPDLOCK) SET SettingValue = CAST(CAST(ISNULL(NULLIF(SettingValue,''),'0') AS INT)+1 AS NVARCHAR(50)) WHERE SettingName=@N; " +
                "SELECT CAST(SettingValue AS INT)-1 FROM Settings WHERE SettingName=@N;", conn, trans))
            {
                cmd.Parameters.AddWithValue("@N", name);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
