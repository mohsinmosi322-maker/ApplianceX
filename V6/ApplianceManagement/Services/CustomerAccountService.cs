using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;

namespace ApplianceManagement.Services
{
    public class LedgerEntry
    {
        public DateTime EntryDate { get; set; }
        public string EntryType { get; set; }
        public string ReferenceNo { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Remarks { get; set; }
        public decimal RunningBalance { get; set; }
    }

    public class CustomerAccountService
    {
        public decimal GetBalance(int customerId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                try
                {
                    using (var cmd = DbHelper.CreateCommand(
                        "SELECT ISNULL(SUM(Debit),0)-ISNULL(SUM(Credit),0) FROM CustomerLedger WHERE CustomerID=@C", conn))
                    {
                        cmd.Parameters.AddWithValue("@C", customerId);
                        return Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
                catch (SqlException) { return 0; }
            }
        }

        public List<LedgerEntry> GetLedger(int customerId, DateTime from, DateTime to)
        {
            var list = new List<LedgerEntry>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                try
                {
                    using (var cmd = DbHelper.CreateCommand(
                        "SELECT EntryDate,EntryType,ReferenceNo,Debit,Credit,Remarks FROM CustomerLedger " +
                        "WHERE CustomerID=@C AND CAST(EntryDate AS DATE) BETWEEN @F AND @T ORDER BY EntryDate, LedgerID", conn))
                    {
                        cmd.Parameters.AddWithValue("@C", customerId);
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
                                run += e.Debit - e.Credit;
                                e.RunningBalance = run;
                                list.Add(e);
                            }
                    }
                }
                catch (SqlException) { }
            }
            return list;
        }

        public void RecordPayment(int customerId, decimal amount, string remarks)
        {
            if (amount <= 0) throw new InvalidOperationException("Payment amount must be > 0.");
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO CustomerLedger(CustomerID,EntryDate,EntryType,ReferenceNo,Debit,Credit,Remarks,CreatedBy) " +
                    "VALUES(@C,GETDATE(),'PAYMENT',NULL,0,@A,@R,@By)", conn))
                {
                    cmd.Parameters.AddWithValue("@C", customerId);
                    cmd.Parameters.AddWithValue("@A", amount);
                    cmd.Parameters.AddWithValue("@R", (object)remarks ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@By", AppSession.UserId > 0 ? (object)AppSession.UserId : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void PostSale(SqlConnection conn, SqlTransaction trans, int customerId, int saleId, string invoiceNo, decimal net)
        {
            try
            {
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO CustomerLedger(CustomerID,EntryDate,EntryType,ReferenceID,ReferenceNo,Debit,Credit,Remarks,CreatedBy) " +
                    "VALUES(@C,GETDATE(),'SALE',@R,@No,@A,0,'Sale',@By)", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@C", customerId);
                    cmd.Parameters.AddWithValue("@R", saleId);
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
