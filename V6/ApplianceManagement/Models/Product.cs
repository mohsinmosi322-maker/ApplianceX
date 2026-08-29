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
    /// <summary>Pack size; unit sale price = SalePrice / PackSize when PackSize &gt; 0 and != 1</summary>
    public decimal PackSize { get; set; }
    public decimal StockValue { get { return CurrentStock * PurchasePrice; } }
    /// <summary>Price charged per 1 unit in Sale / Sale Return.</summary>
    public decimal UnitSalePrice {
      get {
        decimal pack = PackSize <= 0 ? 1m : PackSize;
        if (pack == 1m) return SalePrice;
        return Math.Round(SalePrice / pack, 4);
      }
    }
    public override string ToString() {
      string u = string.IsNullOrEmpty(UnitOfMeasure) ? "" : (" /" + UnitOfMeasure);
      string pack = PackSize > 1m ? (" pack:" + PackSize.ToString("0.####")) : "";
      return ProductCode + " - " + ProductName + u + pack + " (Stock:" + CurrentStock + ")";
    }
  }
}
