namespace ApplianceManagement.Models {
  public class SaleDetail {
    public int SaleDetailID { get; set; }
    public int SaleID { get; set; }
    public int ProductID { get; set; }
    public string ProductCode { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal SalePrice { get; set; }
    public decimal Discount { get; set; }
    public decimal Amount { get; set; }
  }
}
