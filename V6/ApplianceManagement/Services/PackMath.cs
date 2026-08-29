using System;
using ApplianceManagement.Models;

namespace ApplianceManagement.Services
{
    /// <summary>
    /// Single place for pack ↔ base-unit and pack-price ↔ unit-price math.
    /// See DOMAIN_MODEL.md.
    /// </summary>
    public static class PackMath
    {
        public static decimal NormalizePackSize(decimal packSize)
        {
            if (packSize <= 0m) return 1m;
            return packSize;
        }

        public static decimal NormalizePackSize(Product p)
        {
            if (p == null) return 1m;
            return NormalizePackSize(p.PackSize);
        }

        /// <summary>Purchase/Sale list price is pack price → unit sale price.</summary>
        public static decimal UnitSalePrice(decimal packSalePrice, decimal packSize)
        {
            decimal pack = NormalizePackSize(packSize);
            if (pack == 1m) return packSalePrice;
            return Math.Round(packSalePrice / pack, 4);
        }

        public static decimal UnitSalePrice(Product p)
        {
            if (p == null) return 0m;
            return UnitSalePrice(p.SalePrice, p.PackSize);
        }

        /// <summary>Pack purchase price → unit cost for ledger/COGS.</summary>
        public static decimal UnitCost(decimal packPurchasePrice, decimal packSize)
        {
            decimal pack = NormalizePackSize(packSize);
            if (pack == 1m) return packPurchasePrice;
            return Math.Round(packPurchasePrice / pack, 4);
        }

        public static decimal UnitCost(Product p)
        {
            if (p == null) return 0m;
            return UnitCost(p.PurchasePrice, p.PackSize);
        }

        /// <summary>Purchase packs → base units for inventory.</summary>
        public static int PacksToUnits(int packs, decimal packSize)
        {
            if (packs < 0) packs = 0;
            decimal pack = NormalizePackSize(packSize);
            int units = (int)Math.Round(packs * pack);
            if (units < 0) units = 0;
            return units;
        }

        public static int PacksToUnits(int packs, Product p)
        {
            return PacksToUnits(packs, p == null ? 1m : p.PackSize);
        }
    }
}
