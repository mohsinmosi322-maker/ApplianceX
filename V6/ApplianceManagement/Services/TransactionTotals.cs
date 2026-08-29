using System;

namespace ApplianceManagement.Services
{
    /// <summary>Central recalculation for Sale/Purchase totals (business layer).</summary>
    public static class TransactionTotals
    {
        public struct Result
        {
            public decimal Gross;
            public decimal DiscountAmount;
            public decimal DiscountPercent;
            public decimal Net;
            public decimal Paid;
            public decimal Balance;
        }

        public static Result Calculate(decimal gross, decimal discountAmount, decimal paid)
        {
            if (gross < 0) gross = 0;
            if (discountAmount < 0) discountAmount = 0;
            if (discountAmount > gross) discountAmount = gross;
            if (paid < 0) paid = 0;

            decimal net = Math.Round(gross - discountAmount, 2);
            decimal pct = gross > 0 ? Math.Round(discountAmount * 100m / gross, 2) : 0;
            decimal balance = Math.Round(net - paid, 2);

            return new Result
            {
                Gross = Math.Round(gross, 2),
                DiscountAmount = Math.Round(discountAmount, 2),
                DiscountPercent = pct,
                Net = net,
                Paid = Math.Round(paid, 2),
                Balance = balance
            };
        }

        public static Result FromPercent(decimal gross, decimal discountPercent, decimal paid)
        {
            if (discountPercent < 0) discountPercent = 0;
            if (discountPercent > 100) discountPercent = 100;
            decimal disc = Math.Round(gross * discountPercent / 100m, 2);
            return Calculate(gross, disc, paid);
        }
    }
}
