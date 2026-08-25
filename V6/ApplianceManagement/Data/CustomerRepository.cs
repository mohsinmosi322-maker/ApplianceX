
using System; using System.Data.SqlClient; using ApplianceManagement.Helpers; using ApplianceManagement.Models;
namespace ApplianceManagement.Data {
  public class CustomerRepository {
    public Customer GetWalkInCustomer() {
      using (var conn = DbHelper.GetConnection()) {
        conn.Open();
        using (var cmd = DbHelper.CreateCommand("SELECT * FROM Customers WHERE CustomerName='Walk-in Customer' AND IsActive=1", conn))
        using (var r = cmd.ExecuteReader()) {
          if (r.Read()) return new Customer { CustomerID=(int)r["CustomerID"], CustomerName=r["CustomerName"].ToString(),
            Phone=r["Phone"]==DBNull.Value?null:r["Phone"].ToString(), Address=r["Address"]==DBNull.Value?null:r["Address"].ToString(),
            OpeningBalance=(decimal)r["OpeningBalance"], IsActive=(bool)r["IsActive"], CreatedDate=(DateTime)r["CreatedDate"] };
        }
      }
      return null;
    }
    public int CountActive() {
      using (var conn = DbHelper.GetConnection()) {
        conn.Open();
        using (var cmd = DbHelper.CreateCommand("SELECT COUNT(1) FROM Customers WHERE IsActive=1", conn))
          return Convert.ToInt32(cmd.ExecuteScalar());
      }
    }
  }
}
