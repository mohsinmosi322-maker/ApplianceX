using System;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Services
{
    /// <summary>Opening / Adjustment / Damage — always via InventoryTransaction.</summary>
    public class StockOpsService
    {
        private readonly InventoryService _inv = new InventoryService();

        public void Opening(int productId, int units, decimal unitCost, string reason)
        {
            if (units <= 0) throw new InvalidOperationException("Opening qty must be > 0.");
            if (string.IsNullOrWhiteSpace(reason)) reason = "Opening stock";
            Post(productId, InventoryTransactionType.Opening, units, 0, unitCost, reason);
        }

        public void Adjust(int productId, int unitsDelta, decimal unitCost, string reason)
        {
            if (unitsDelta == 0) throw new InvalidOperationException("Adjustment cannot be zero.");
            if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Reason required.");
            if (unitsDelta > 0)
                Post(productId, InventoryTransactionType.Adjustment, unitsDelta, 0, unitCost, reason);
            else
                Post(productId, InventoryTransactionType.Adjustment, 0, -unitsDelta, unitCost, reason);
        }

        public void Damage(int productId, int units, decimal unitCost, string reason)
        {
            if (units <= 0) throw new InvalidOperationException("Damage qty must be > 0.");
            if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("Reason required.");
            Post(productId, InventoryTransactionType.Damage, 0, units, unitCost, reason);
        }

        private void Post(int productId, string type, int qtyIn, int qtyOut, decimal unitCost, string reason)
        {
            AppSession.RequirePermission("INVENTORY");
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        if (qtyOut > 0)
                            _inv.EnsureStock(conn, trans, productId, qtyOut, null);
                        string rem = reason + " | user=" + AppSession.UserName;
                        _inv.Post(conn, trans, productId, type, null, qtyIn, qtyOut, unitCost, rem);
                        trans.Commit();
                        AppLog.Info(type + " product=" + productId + " in=" + qtyIn + " out=" + qtyOut);
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }
    }
}
