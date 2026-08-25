using System;
namespace ApplianceManagement.Models {
  public class Customer {
    public int CustomerID { get; set; }
    public string CustomerName { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public decimal OpeningBalance { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
  }
}
