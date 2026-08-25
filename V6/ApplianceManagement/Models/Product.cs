using System;
namespace ApplianceManagement.Models {
  public class Product {
    public int ProductID { get; set; }
    public string ProductCode { get; set; }
    public string Barcode { get; set; }
    public string ProductName { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int MinimumStock { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public int CurrentStock { get; set; }
    public decimal StockValue { get { return CurrentStock * PurchasePrice; } }
    public override string ToString() { return ProductCode + " - " + ProductName + " (Stock:" + CurrentStock + ")"; }
  }
}
