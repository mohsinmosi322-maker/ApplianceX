
using System; using System.Collections.Generic; using System.Data.SqlClient; using ApplianceManagement.Helpers; using ApplianceManagement.Models;
namespace ApplianceManagement.Data {
  public class SupplierRepository {
    public List<Supplier> GetAllActive() {
      var list = new List<Supplier>();
      using (var conn = DbHelper.GetConnection()) {
        conn.Open();
        using (var cmd = DbHelper.CreateCommand("SELECT * FROM Suppliers WHERE IsActive=1 ORDER BY SupplierName", conn))
        using (var r = cmd.ExecuteReader())
          while (r.Read()) list.Add(new Supplier { SupplierID=(int)r["SupplierID"], SupplierName=r["SupplierName"].ToString(),
            Phone=r["Phone"]==DBNull.Value?null:r["Phone"].ToString(), Address=r["Address"]==DBNull.Value?null:r["Address"].ToString(),
            OpeningBalance=(decimal)r["OpeningBalance"], IsActive=(bool)r["IsActive"], CreatedDate=(DateTime)r["CreatedDate"] });
      }
      return list;
    }
    public int CountActive() {
      using (var conn = DbHelper.GetConnection()) {
        conn.Open();
        using (var cmd = DbHelper.CreateCommand("SELECT COUNT(1) FROM Suppliers WHERE IsActive=1", conn))
          return Convert.ToInt32(cmd.ExecuteScalar());
      }
    }
    public int Insert(Supplier s) {
      using (var conn = DbHelper.GetConnection()) {
        conn.Open();
        using (var cmd = DbHelper.CreateCommand("INSERT INTO Suppliers(SupplierName,Phone,Address,OpeningBalance,IsActive) VALUES(@N,@P,@A,0,1); SELECT SCOPE_IDENTITY();", conn)) {
          cmd.Parameters.AddWithValue("@N", s.SupplierName); cmd.Parameters.AddWithValue("@P", (object)s.Phone??DBNull.Value);
          cmd.Parameters.AddWithValue("@A", (object)s.Address??DBNull.Value); return Convert.ToInt32(cmd.ExecuteScalar());
        }
      }
    }
  }
}
