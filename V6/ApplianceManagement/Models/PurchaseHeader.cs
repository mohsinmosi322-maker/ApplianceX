using System;
using System.Collections.Generic;
namespace ApplianceManagement.Models {
  public class PurchaseHeader {
    public int PurchaseID { get; set; }
    public string InvoiceNo { get; set; }
    public DateTime PurchaseDate { get; set; }
    public int SupplierID { get; set; }
    public string SupplierName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Remarks { get; set; }
    public List<PurchaseDetail> Details { get; set; } = new List<PurchaseDetail>();
  }
}
