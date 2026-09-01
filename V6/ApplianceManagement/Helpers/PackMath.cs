using System;
using ApplianceManagement.Models;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Pack-size aware pricing.
    /// Sale: inventory is in base units; unit sale price = pack SalePrice / PackSize.
    /// Purchase: quantities are packs; line price = full pack PurchasePrice.
    /// </summary>
    public static class PackMath
    {
        public static decimal EffectivePackSize(Product p)
        {
            if (p == null) return 1m;
            return p.PackSize > 0 ? p.PackSize : 1m;
        }

        public static decimal UnitSalePrice(Product p)
        {
            if (p == null) return 0m;
            decimal pack = EffectivePackSize(p);
            if (pack <= 1m) return Math.Round(p.SalePrice, 4);
            return Math.Round(p.SalePrice / pack, 4);
        }

        public static decimal PackPurchasePrice(Product p)
        {
            if (p == null) return 0m;
            return Math.Round(p.PurchasePrice, 4);
        }
    }
}
