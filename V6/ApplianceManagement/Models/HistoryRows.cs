using System;

namespace ApplianceManagement.Models
{
    public class ProductSaleHistoryRow
    {
        public DateTime Date { get; set; }
        public string Invoice { get; set; }
        public string Customer { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
    }

    public class ProductPurchaseHistoryRow
    {
        public DateTime Date { get; set; }
        public string Invoice { get; set; }
        public string Supplier { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
    }
}
