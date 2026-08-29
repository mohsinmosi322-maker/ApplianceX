using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Services
{
    public class SupplierAccountService
    {
        public decimal GetBalance(int supplierId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                try
                {
                    // Credit = payable increase (purchase), Debit = payment/return
                    using (var cmd = DbHelper.CreateCommand(
                        "SELECT ISNULL(SUM(Credit),0)-ISNULL(SUM(Debit),0) FROM SupplierLedger WHERE SupplierID=@S", conn))
                    {
                        cmd.Parameters.AddWithValue("@S", supplierId);
                        return Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
                catch (SqlException) { return 0; }
            }
        }

        public List<LedgerEntry> GetLedger(int supplierId, DateTime from, DateTime to)
        {
            var list = new List<LedgerEntry>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                try
                {
                    using (var cmd = DbHelper.CreateCommand(
                        "SELECT EntryDate,EntryType,ReferenceNo,Debit,Credit,Remarks FROM SupplierLedger " +
                        "WHERE SupplierID=@S AND CAST(EntryDate AS DATE) BETWEEN @F AND @T ORDER BY EntryDate, LedgerID", conn))
                    {
                        cmd.Parameters.AddWithValue("@S", supplierId);
                        cmd.Parameters.AddWithValue("@F", from.Date);
                        cmd.Parameters.AddWithValue("@T", to.Date);
                        decimal run = 0;
                        using (var r = cmd.ExecuteReader())
                            while (r.Read())
                            {
                                var e = new LedgerEntry
                                {
                                    EntryDate = (DateTime)r["EntryDate"],
                                    EntryType = r["EntryType"].ToString(),
                                    ReferenceNo = r["ReferenceNo"] == DBNull.Value ? "" : r["ReferenceNo"].ToString(),
                                    Debit = Convert.ToDecimal(r["Debit"]),
                                    Credit = Convert.ToDecimal(r["Credit"]),
                                    Remarks = r["Remarks"] == DBNull.Value ? "" : r["Remarks"].ToString()
                                };
                                run += e.Credit - e.Debit;
                                e.RunningBalance = run;
                                list.Add(e);
                            }
                    }
                }
                catch (SqlException) { }
            }
            return list;
        }

        public void RecordPayment(int supplierId, decimal amount, string remarks)
        {
            if (amount <= 0) throw new InvalidOperationException("Payment amount must be > 0.");
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO SupplierLedger(SupplierID,EntryDate,EntryType,ReferenceNo,Debit,Credit,Remarks,CreatedBy) " +
                    "VALUES(@S,GETDATE(),'PAYMENT',NULL,@A,0,@R,@By)", conn))
                {
                    cmd.Parameters.AddWithValue("@S", supplierId);
                    cmd.Parameters.AddWithValue("@A", amount);
                    cmd.Parameters.AddWithValue("@R", (object)remarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void PostPurchase(SqlConnection conn, SqlTransaction trans, int supplierId, int purchaseId, string invoiceNo, decimal net)
        {
            try
            {
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO SupplierLedger(SupplierID,EntryDate,EntryType,ReferenceID,ReferenceNo,Debit,Credit,Remarks,CreatedBy) " +
                    "VALUES(@S,GETDATE(),'PURCHASE',@R,@No,0,@A,'Purchase',@By)", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@S", supplierId);
                    cmd.Parameters.AddWithValue("@R", purchaseId);
                    cmd.Parameters.AddWithValue("@No", invoiceNo);
                    cmd.Parameters.AddWithValue("@A", net);
                    cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException) { }
        }
    }
}
