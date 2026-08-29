using System;
using System.Collections.Generic;

namespace ApplianceManagement.Models
{
    public class SaleReturnHeader
    {
        public int SaleReturnID { get; set; }
        public string ReturnNo { get; set; }
        public DateTime ReturnDate { get; set; }
        public int OriginalSaleID { get; set; }
        public string OriginalInvoiceNo { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string Remarks { get; set; }
        public List<SaleReturnDetail> Details { get; set; } = new List<SaleReturnDetail>();
    }

    public class SaleReturnDetail
    {
        public int SaleReturnDetailID { get; set; }
        public int SaleReturnID { get; set; }
        public int? OriginalSaleDetailID { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal Amount { get; set; }

        // UI helpers (not always persisted)
        public int SoldQty { get; set; }
        public int AlreadyReturned { get; set; }
        public int ReturnableQty { get { return SoldQty - AlreadyReturned; } }
    }

    /// <summary>One line from original invoice with returnable qty.</summary>
    public class SaleInvoiceLine
    {
        public int SaleDetailID { get; set; }
        public int SaleID { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime SaleDate { get; set; }
        public int CustomerID { get; set; }
        public string CustomerName { get; set; }
        public int ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int SoldQty { get; set; }
        public int AlreadyReturned { get; set; }
        public int ReturnableQty { get { return Math.Max(0, SoldQty - AlreadyReturned); } }
        public decimal SalePrice { get; set; }
        public decimal Amount { get; set; }
    }
}
