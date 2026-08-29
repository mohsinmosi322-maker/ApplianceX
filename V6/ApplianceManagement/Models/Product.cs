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
    /// <summary>e.g. Piece, Kilograms, Grams — empty = plain unit</summary>
    public string UnitOfMeasure { get; set; }
    /// <summary>Pack size; unit sale price = SalePrice / PackSize when PackSize &gt; 1</summary>
    public decimal PackSize { get; set; }
    public decimal StockValue { get { return CurrentStock * PurchasePrice; } }
    public decimal UnitSalePrice {
      get {
        if (PackSize > 1m) return Math.Round(SalePrice / PackSize, 4);
        return SalePrice;
      }
    }
    public override string ToString() {
      string u = string.IsNullOrEmpty(UnitOfMeasure) ? "" : (" /" + UnitOfMeasure);
      return ProductCode + " - " + ProductName + u + " (Stock:" + CurrentStock + ")";
    }
  }
}
