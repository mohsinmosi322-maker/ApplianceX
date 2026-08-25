using System;
namespace ApplianceManagement.Models {
  public class Supplier {
    public int SupplierID { get; set; }
    public string SupplierName { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public override string ToString() { return SupplierName; }
  }
}
