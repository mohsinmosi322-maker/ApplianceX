using System;
namespace ApplianceManagement.Models {
  public class InventoryTransaction {
    public int InventoryTransactionID { get; set; }
    public DateTime TransactionDate { get; set; }
    public int ProductID { get; set; }
    public string TransactionType { get; set; }
    public int? ReferenceID { get; set; }
    public int QuantityIn { get; set; }
    public int QuantityOut { get; set; }
    public decimal? UnitCost { get; set; }
    public string Remarks { get; set; }
  }
  public static class InventoryTransactionType {
    public const string Purchase = "PURCHASE";
    public const string Sale = "SALE";
    public const string SaleReturn = "SALE_RETURN";
  }
}
