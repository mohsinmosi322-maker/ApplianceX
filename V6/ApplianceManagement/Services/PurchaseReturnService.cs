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
        public int PurchasedQty { get; set; }
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
        public int Quantity { get; set; }
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

            // Creates PurchaseReturnHeader/Detail if missing
            SchemaBootstrap.EnsurePurchaseReturnTables();

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

            if (list.Count == 0)
                throw new InvalidOperationException("Invoice not found: " + invoiceNo.Trim());

            return list;
        }

        public void Validate(PurchaseReturnHeader ret)
        {
            if (ret == null) throw new ArgumentNullException("ret");
            if (ret.OriginalPurchaseID <= 0)
                throw new InvalidOperationException("Load an original purchase invoice first.");
            if (ret.Details == null || ret.Details.Count == 0)
                throw new InvalidOperationException("Select at least one line to return.");
            if (string.IsNullOrWhiteSpace(ret.Remarks))
                throw new InvalidOperationException("Return reason is required.");

            decimal gross = 0;
            foreach (var d in ret.Details)
            {
                if (d.Quantity <= 0)
                    throw new InvalidOperationException("Return qty must be > 0.");
                if (d.ProductID <= 0)
                    throw new InvalidOperationException("Invalid product on return line.");
                d.Amount = Math.Round(d.Quantity * d.PurchasePrice, 2);
                gross += d.Amount;
            }
            ret.TotalAmount = gross;
            ret.NetAmount = Math.Round(gross - ret.Discount, 2);
            if (ret.RefundAmount <= 0)
                ret.RefundAmount = ret.NetAmount;
            if (ret.ReturnDate == default(DateTime))
                ret.ReturnDate = DateTime.Now;
        }

        public int Save(PurchaseReturnHeader ret)
        {
            SchemaBootstrap.EnsurePurchaseReturnTables();
            Validate(ret);

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        int n = InvoiceNumberHelper.NextCounter(conn, trans, "NextPurchaseReturnNumber");
                        string returnNo = InvoiceNumberHelper.Format("", n);
                        ret.ReturnNo = returnNo;

                        int id;
                        using (var cmd = DbHelper.CreateCommand(
                            "INSERT INTO PurchaseReturnHeader(ReturnNo,ReturnDate,OriginalPurchaseID,SupplierID,TotalAmount,Discount,NetAmount,RefundAmount,Remarks,CreatedBy) " +
                            "VALUES(@No,@Dt,@Oid,@Sid,@Tot,@Disc,@Net,@Ref,@Rm,@By); SELECT SCOPE_IDENTITY();", conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@No", returnNo);
                            cmd.Parameters.AddWithValue("@Dt", ret.ReturnDate);
                            cmd.Parameters.AddWithValue("@Oid", ret.OriginalPurchaseID);
                            cmd.Parameters.AddWithValue("@Sid", ret.SupplierID);
                            cmd.Parameters.AddWithValue("@Tot", ret.TotalAmount);
                            cmd.Parameters.AddWithValue("@Disc", ret.Discount);
                            cmd.Parameters.AddWithValue("@Net", ret.NetAmount);
                            cmd.Parameters.AddWithValue("@Ref", ret.RefundAmount);
                            cmd.Parameters.AddWithValue("@Rm", (object)ret.Remarks ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                            id = Convert.ToInt32(cmd.ExecuteScalar());
                            ret.PurchaseReturnID = id;
                        }

                        foreach (var d in ret.Details)
                        {
                            int originalDetailId = d.OriginalPurchaseDetailID.HasValue ? d.OriginalPurchaseDetailID.Value : 0;
                            if (originalDetailId <= 0)
                                throw new InvalidOperationException("Original purchase line required for " + (d.ProductName ?? d.ProductCode));

                            int returnable = GetReturnable(conn, trans, originalDetailId);
                            if (d.Quantity > returnable)
                                throw new InvalidOperationException(
                                    "Return qty exceeds remaining for " + (d.ProductName ?? d.ProductCode) +
                                    ". Returnable: " + returnable);

                            using (var cmd = DbHelper.CreateCommand(
                                "INSERT INTO PurchaseReturnDetail(PurchaseReturnID,OriginalPurchaseDetailID,ProductID,Quantity,PurchasePrice,Amount) " +
                                "VALUES(@Rid,@Oid,@Pid,@Qty,@Pr,@Amt)", conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Rid", id);
                                cmd.Parameters.AddWithValue("@Oid", originalDetailId);
                                cmd.Parameters.AddWithValue("@Pid", d.ProductID);
                                cmd.Parameters.AddWithValue("@Qty", d.Quantity);
                                cmd.Parameters.AddWithValue("@Pr", d.PurchasePrice);
                                cmd.Parameters.AddWithValue("@Amt", d.Amount);
                                cmd.ExecuteNonQuery();
                            }

                            // Purchase return DECREASES stock (pack → base units)
                            decimal pack = d.PackSize > 0 ? d.PackSize : 1m;
                            int unitsOut = (int)Math.Round(d.Quantity * pack);
                            if (unitsOut < 1) unitsOut = d.Quantity;

                            _inv.EnsureStock(conn, trans, d.ProductID, unitsOut, d.ProductName);
                            _inv.Post(
                                conn, trans, d.ProductID,
                                InventoryTransactionType.PurchaseReturn,
                                id,
                                0, unitsOut, d.PurchasePrice,
                                "Purchase return " + returnNo + " for " + (ret.OriginalInvoiceNo ?? ""),
                                ret.ReturnDate);
                        }

                        if (ret.SupplierID > 0 && ret.NetAmount > 0)
                        {
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
                                    cmd.Parameters.AddWithValue("@Rem", (object)ret.Remarks ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch (SqlException) { /* ledger optional */ }
                        }

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

        private static int GetReturnable(SqlConnection conn, SqlTransaction trans, int purchaseDetailId)
        {
            using (var cmd = DbHelper.CreateCommand(
                "SELECT d.Quantity - ISNULL((SELECT SUM(rd.Quantity) FROM PurchaseReturnDetail rd WHERE rd.OriginalPurchaseDetailID=d.PurchaseDetailID),0) " +
                "FROM PurchaseDetail d WITH (UPDLOCK, ROWLOCK) WHERE d.PurchaseDetailID=@ID", conn, trans))
            {
                cmd.Parameters.AddWithValue("@ID", purchaseDetailId);
                var o = cmd.ExecuteScalar();
                if (o == null || o == DBNull.Value) return 0;
                int n = Convert.ToInt32(o);
                return n < 0 ? 0 : n;
            }
        }
    }
}
