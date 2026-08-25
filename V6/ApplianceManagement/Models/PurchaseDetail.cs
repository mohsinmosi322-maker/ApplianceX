namespace ApplianceManagement.Models {
  public class PurchaseDetail {
    public int PurchaseDetailID { get; set; }
    public int PurchaseID { get; set; }
    public int ProductID { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Amount { get; set; }
  }
}
