using System;
using System.Collections.Generic;
using ApplianceManagement.Data;
using ApplianceManagement.Models;

namespace ApplianceManagement.Services
{
    /// <summary>
    /// Business rules for purchases.
    /// Line Quantity = packs; stock impact = packs × PackSize (handled in repository via PackMath).
    /// Line PurchasePrice = pack price.
    /// </summary>
    public class PurchaseService
    {
        private readonly PurchaseRepository _repo = new PurchaseRepository();

        public void ValidateAndNormalize(PurchaseHeader purchase)
        {
            if (purchase == null) throw new ArgumentNullException("purchase");
            if (purchase.Details == null || purchase.Details.Count == 0)
                throw new InvalidOperationException("Add at least one product line.");
            if (purchase.SupplierID <= 0)
                throw new InvalidOperationException("Select a supplier.");

            decimal gross = 0;
            foreach (var d in purchase.Details)
            {
                if (d.ProductID <= 0)
                    throw new InvalidOperationException("Invalid product on a line.");
                if (d.Quantity <= 0)
                    throw new InvalidOperationException(
                        "Pack quantity must be > 0 for " + (d.ProductName ?? d.ProductCode ?? "product") + ".");
                if (d.PurchasePrice < 0)
                    throw new InvalidOperationException("Purchase price cannot be negative.");
                d.Amount = Math.Round(d.Quantity * d.PurchasePrice - d.Discount, 2);
                if (d.Amount < 0) d.Amount = 0;
                gross += d.Amount;
            }

            var totals = TransactionTotals.Calculate(gross, purchase.Discount, purchase.PaidAmount);
            if (totals.DiscountAmount > totals.Gross)
                throw new InvalidOperationException("Discount cannot exceed total.");
            if (totals.Paid < 0)
                throw new InvalidOperationException("Paid amount cannot be negative.");

            purchase.TotalAmount = totals.Gross;
            purchase.Discount = totals.DiscountAmount;
            purchase.NetAmount = totals.Net;
            purchase.PaidAmount = totals.Paid;
            purchase.BalanceAmount = totals.Balance;
            if (purchase.PurchaseDate == default(DateTime))
                purchase.PurchaseDate = DateTime.Now;
        }

        public int Save(PurchaseHeader purchase)
        {
            ValidateAndNormalize(purchase);
            return _repo.SavePurchase(purchase);
        }

        public List<PurchaseHeader> GetPurchases(DateTime from, DateTime to) =>
            _repo.GetPurchases(from, to);

        public List<ProductPurchaseHistoryRow> GetProductHistory(int productId) =>
            _repo.GetProductPurchaseHistory(productId);

        public void UpdatePayment(int purchaseId, decimal paid, decimal balance, string remarks) =>
            _repo.UpdateHeader(purchaseId, paid, balance, remarks);
    }
}
