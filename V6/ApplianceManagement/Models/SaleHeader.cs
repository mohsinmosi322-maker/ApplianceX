using System;
using System.Collections.Generic;
namespace ApplianceManagement.Models {
  public class SaleHeader {
    public int SaleID { get; set; }
    public string InvoiceNo { get; set; }
    public DateTime SaleDate { get; set; }
    public int CustomerID { get; set; }
    public string CustomerName { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Discount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }
    public string Remarks { get; set; }
    public List<SaleDetail> Details { get; set; } = new List<SaleDetail>();
  }
}
