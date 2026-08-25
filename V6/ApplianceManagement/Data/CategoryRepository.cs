
using System.Collections.Generic; using System.Data.SqlClient; using ApplianceManagement.Helpers; using ApplianceManagement.Models;
namespace ApplianceManagement.Data {
  public class CategoryRepository {
    public List<Category> GetAllActive() {
      var list = new List<Category>();
      using (var conn = DbHelper.GetConnection()) {
        conn.Open();
        using (var cmd = DbHelper.CreateCommand("SELECT * FROM Categories WHERE IsActive=1 ORDER BY CategoryName", conn))
        using (var r = cmd.ExecuteReader())
          while (r.Read()) list.Add(new Category { CategoryID=(int)r["CategoryID"], CategoryName=r["CategoryName"].ToString(), IsActive=(bool)r["IsActive"] });
      }
      return list;
    }
  }
}
