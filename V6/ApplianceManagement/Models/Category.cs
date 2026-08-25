namespace ApplianceManagement.Models {
  public class Category {
    public int CategoryID { get; set; }
    public string CategoryName { get; set; }
    public bool IsActive { get; set; }
    public override string ToString() { return CategoryName; }
  }
}
