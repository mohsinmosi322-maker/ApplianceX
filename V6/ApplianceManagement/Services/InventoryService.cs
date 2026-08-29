using System;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Services
{
    /// <summary>
    /// InventoryTransaction is the only source of truth for stock.
    /// CurrentStock is updated only via SyncCacheFromLedger.
    /// </summary>
    public class InventoryService
    {
        public int GetAvailableUnits(int productId)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                return GetAvailableUnits(conn, null, productId);
            }
        }

        public int GetAvailableUnits(SqlConnection conn, SqlTransaction trans, int productId)
        {
            using (var cmd = DbHelper.CreateCommand(
                "SELECT ISNULL(SUM(QuantityIn),0) - ISNULL(SUM(QuantityOut),0) " +
                "FROM InventoryTransaction WITH (UPDLOCK) WHERE ProductID=@P", conn, trans))
            {
                cmd.Parameters.AddWithValue("@P", productId);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void EnsureStock(SqlConnection conn, SqlTransaction trans, int productId, int requiredUnits, string productName)
        {
            int available = GetAvailableUnits(conn, trans, productId);
            if (available < requiredUnits)
                throw new InvalidOperationException(
                    "Insufficient stock for " + (productName ?? ("Product " + productId)) +
                    ". Available: " + available + ", requested: " + requiredUnits);
        }

        public void Post(
            SqlConnection conn,
            SqlTransaction trans,
            int productId,
            string transactionType,
            int? referenceId,
            int quantityIn,
            int quantityOut,
            decimal unitCost,
            string remarks,
            DateTime? when = null)
        {
            if (quantityIn < 0 || quantityOut < 0)
                throw new ArgumentException("QuantityIn/Out cannot be negative.");
            if (quantityIn == 0 && quantityOut == 0)
                throw new ArgumentException("Inventory post requires non-zero quantity.");

            using (var cmd = DbHelper.CreateCommand(
                "INSERT INTO InventoryTransaction(TransactionDate,ProductID,TransactionType,ReferenceID,QuantityIn,QuantityOut,UnitCost,Remarks) " +
                "VALUES(@Dt,@P,@T,@R,@In,@Out,@C,@Rem)", conn, trans))
            {
                cmd.Parameters.AddWithValue("@Dt", when ?? DateTime.Now);
                cmd.Parameters.AddWithValue("@P", productId);
                cmd.Parameters.AddWithValue("@T", transactionType);
                cmd.Parameters.AddWithValue("@R", (object)referenceId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@In", quantityIn);
                cmd.Parameters.AddWithValue("@Out", quantityOut);
                cmd.Parameters.AddWithValue("@C", unitCost);
                cmd.Parameters.AddWithValue("@Rem", (object)remarks ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }

            SyncCacheFromLedger(conn, trans, productId);
        }

        /// <summary>Rewrite Products.CurrentStock from ledger for one product (cache).</summary>
        public void SyncCacheFromLedger(SqlConnection conn, SqlTransaction trans, int productId)
        {
            try
            {
                using (var cmd = DbHelper.CreateCommand(
                    "UPDATE Products SET CurrentStock = ISNULL((" +
                    "  SELECT SUM(QuantityIn)-SUM(QuantityOut) FROM InventoryTransaction WHERE ProductID=@P),0) " +
                    "WHERE ProductID=@P", conn, trans))
                {
                    cmd.Parameters.AddWithValue("@P", productId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException)
            {
                // Column may not exist on very old DBs
            }
        }

        public void SyncAllCaches()
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                try
                {
                    using (var cmd = DbHelper.CreateCommand(
                        "UPDATE p SET CurrentStock = ISNULL(x.Bal,0) " +
                        "FROM Products p OUTER APPLY (" +
                        "  SELECT SUM(QuantityIn)-SUM(QuantityOut) AS Bal FROM InventoryTransaction t WHERE t.ProductID=p.ProductID) x", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    AppLog.Error("SyncAllCaches failed", ex);
                }
            }
        }
    }
}
