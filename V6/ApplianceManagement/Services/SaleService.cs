using System;
using System.Collections.Generic;
using System.Linq;
using ApplianceManagement.Data;
using ApplianceManagement.Models;

namespace ApplianceManagement.Services
{
    /// <summary>
    /// Business rules for sales. Forms call this; repositories stay data-only.
    /// Qty on lines = base units; price = unit sale price.
    /// </summary>
    public class SaleService
    {
        private readonly SaleRepository _repo = new SaleRepository();
        private readonly InventoryService _inv = new InventoryService();

        public void ValidateAndNormalize(SaleHeader sale)
        {
            if (sale == null) throw new ArgumentNullException("sale");
            if (sale.Details == null || sale.Details.Count == 0)
                throw new InvalidOperationException("Add at least one product line.");
            if (sale.CustomerID <= 0)
                throw new InvalidOperationException("Select a customer.");

            decimal gross = 0;
            foreach (var d in sale.Details)
            {
                if (d.ProductID <= 0)
                    throw new InvalidOperationException("Invalid product on a line.");
                if (d.Quantity <= 0)
                    throw new InvalidOperationException(
                        "Quantity must be > 0 for " + (d.ProductName ?? d.ProductCode ?? "product") + ".");
                if (d.SalePrice < 0)
                    throw new InvalidOperationException("Sale price cannot be negative.");
                d.Amount = Math.Round(d.Quantity * d.SalePrice - d.Discount, 2);
                if (d.Amount < 0) d.Amount = 0;
                gross += d.Amount;
            }

            var totals = TransactionTotals.Calculate(gross, sale.Discount, sale.PaidAmount);
            if (totals.DiscountAmount > totals.Gross)
                throw new InvalidOperationException("Discount cannot exceed total.");
            if (totals.Paid < 0)
                throw new InvalidOperationException("Paid amount cannot be negative.");

            sale.TotalAmount = totals.Gross;
            sale.Discount = totals.DiscountAmount;
            sale.NetAmount = totals.Net;
            sale.PaidAmount = totals.Paid;
            sale.BalanceAmount = totals.Balance;
            if (sale.SaleDate == default(DateTime))
                sale.SaleDate = DateTime.Now;
        }

        /// <summary>Validate stock before open transaction (UI feedback). Final check still in repo/service save.</summary>
        public void EnsureStockAvailable(IEnumerable<SaleDetail> details)
        {
            if (details == null) return;
            var grouped = details.GroupBy(d => d.ProductID)
                .Select(g => new { ProductID = g.Key, Qty = g.Sum(x => x.Quantity), Name = g.First().ProductName });
            foreach (var g in grouped)
            {
                int available = _inv.GetAvailableUnits(g.ProductID);
                if (available < g.Qty)
                    throw new InvalidOperationException(
                        "Insufficient stock for " + (g.Name ?? ("Product " + g.ProductID)) +
                        ". Available: " + available + ", requested: " + g.Qty);
            }
        }

        public int Save(SaleHeader sale)
        {
            ValidateAndNormalize(sale);
            EnsureStockAvailable(sale.Details);
            return _repo.SaveSale(sale);
        }

        public List<SaleHeader> GetSales(DateTime from, DateTime to) => _repo.GetSales(from, to);

        public List<ProductSaleHistoryRow> GetProductHistory(int productId) =>
            _repo.GetProductSaleHistory(productId);

        public void UpdatePayment(int saleId, decimal paid, decimal balance, string remarks) =>
            _repo.UpdateHeader(saleId, paid, balance, remarks);
    }
}
