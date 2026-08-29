
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Data
{
    public class ProductRepository
    {
        public Product GetByBarcode(string barcode) => GetSingle("p.Barcode=@Val", barcode);
        public Product GetByCode(string code) => GetSingle("p.ProductCode=@Val", code);

        public Product GetById(int id)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(Base() + " WHERE p.ProductID=@ID AND p.IsActive=1", conn))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (var r = cmd.ExecuteReader()) if (r.Read()) return Map(r);
                }
            }
            return null;
        }

        public List<Product> Search(string keyword)
        {
            var list = new List<Product>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(Base() +
                    " WHERE p.IsActive=1 AND (p.ProductCode LIKE @kw OR p.Barcode LIKE @kw OR p.ProductName LIKE @kw) ORDER BY p.ProductName", conn))
                {
                    cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(Map(r));
                }
            }
            return list;
        }

        public List<Product> GetAllActive()
        {
            var list = new List<Product>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(Base() + " WHERE p.IsActive=1 ORDER BY p.ProductName", conn))
                using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(Map(r));
            }
            return list;
        }

        public List<Product> GetLowStock()
        {
            var list = new List<Product>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                string sql = Base() +
                    " WHERE p.IsActive=1 AND ISNULL((SELECT SUM(QuantityIn)-SUM(QuantityOut) FROM InventoryTransaction WHERE ProductID=p.ProductID),0) <= p.MinimumStock ORDER BY p.ProductName";
                using (var cmd = DbHelper.CreateCommand(sql, conn))
                using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(Map(r));
            }
            return list;
        }

        public string GetNextProductCode()
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT ISNULL(MAX(TRY_CAST(ProductCode AS INT)),0)+1 FROM Products WHERE ISNUMERIC(ProductCode)=1", conn))
                    return Convert.ToInt32(cmd.ExecuteScalar()).ToString("D3");
            }
        }

        public int Insert(Product p)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                // Prefer full insert with UOM/PackSize when columns exist
                try
                {
                    using (var cmd = DbHelper.CreateCommand(
                        "INSERT INTO Products(ProductCode,Barcode,ProductName,CategoryID,PurchasePrice,SalePrice,MinimumStock,IsActive,CurrentStock,UnitOfMeasure,PackSize) " +
                        "VALUES(@C,@B,@N,@Cat,@Pur,@Sale,@Min,1,0,@Uom,@Pack); SELECT SCOPE_IDENTITY();", conn))
                    {
                        BindInsert(cmd, p, true);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                catch (SqlException)
                {
                    try
                    {
                        using (var cmd = DbHelper.CreateCommand(
                            "INSERT INTO Products(ProductCode,Barcode,ProductName,CategoryID,PurchasePrice,SalePrice,MinimumStock,IsActive,CurrentStock) " +
                            "VALUES(@C,@B,@N,@Cat,@Pur,@Sale,@Min,1,0); SELECT SCOPE_IDENTITY();", conn))
                        {
                            BindInsert(cmd, p, false);
                            return Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                    catch (SqlException)
                    {
                        using (var cmd = DbHelper.CreateCommand(
                            "INSERT INTO Products(ProductCode,Barcode,ProductName,CategoryID,PurchasePrice,SalePrice,MinimumStock,IsActive) " +
                            "VALUES(@C,@B,@N,@Cat,@Pur,@Sale,@Min,1); SELECT SCOPE_IDENTITY();", conn))
                        {
                            BindInsert(cmd, p, false);
                            return Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }
                }
            }
        }

        private static void BindInsert(SqlCommand cmd, Product p, bool withUom)
        {
            cmd.Parameters.AddWithValue("@C", p.ProductCode);
            cmd.Parameters.AddWithValue("@B", (object)p.Barcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@N", p.ProductName);
            cmd.Parameters.AddWithValue("@Cat", p.CategoryID);
            cmd.Parameters.AddWithValue("@Pur", p.PurchasePrice);
            cmd.Parameters.AddWithValue("@Sale", p.SalePrice);
            cmd.Parameters.AddWithValue("@Min", p.MinimumStock);
            if (withUom)
            {
                cmd.Parameters.AddWithValue("@Uom", string.IsNullOrEmpty(p.UnitOfMeasure) ? (object)DBNull.Value : p.UnitOfMeasure);
                cmd.Parameters.AddWithValue("@Pack", p.PackSize <= 0 ? 1m : p.PackSize);
            }
        }

        public bool ExistsCode(string code)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT COUNT(1) FROM Products WHERE ProductCode=@C", conn))
                {
                    cmd.Parameters.AddWithValue("@C", code);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public bool ExistsBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return false;
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT COUNT(1) FROM Products WHERE Barcode=@B", conn))
                {
                    cmd.Parameters.AddWithValue("@B", barcode);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        private Product GetSingle(string where, string val)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(Base() + " WHERE " + where + " AND p.IsActive=1", conn))
                {
                    cmd.Parameters.AddWithValue("@Val", val);
                    using (var r = cmd.ExecuteReader()) if (r.Read()) return Map(r);
                }
            }
            return null;
        }

        private string Base() =>
            @"SELECT p.*,c.CategoryName,
              ISNULL((SELECT SUM(QuantityIn)-SUM(QuantityOut) FROM InventoryTransaction WHERE ProductID=p.ProductID),0) AS CurrentStock
              FROM Products p LEFT JOIN Categories c ON p.CategoryID=c.CategoryID";

        private static bool HasCol(SqlDataReader r, string name)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (string.Equals(r.GetName(i), name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private Product Map(SqlDataReader r)
        {
            var p = new Product
            {
                ProductID = (int)r["ProductID"],
                ProductCode = r["ProductCode"].ToString(),
                Barcode = r["Barcode"] == DBNull.Value ? null : r["Barcode"].ToString(),
                ProductName = r["ProductName"].ToString(),
                CategoryID = r["CategoryID"] == DBNull.Value ? 0 : (int)r["CategoryID"],
                CategoryName = r["CategoryName"] == DBNull.Value ? "" : r["CategoryName"].ToString(),
                PurchasePrice = (decimal)r["PurchasePrice"],
                SalePrice = (decimal)r["SalePrice"],
                MinimumStock = r["MinimumStock"] == DBNull.Value ? 0 : (int)r["MinimumStock"],
                CurrentStock = Convert.ToInt32(r["CurrentStock"]),
                IsActive = (bool)r["IsActive"],
                CreatedDate = (DateTime)r["CreatedDate"],
                PackSize = 1m
            };
            if (HasCol(r, "UnitOfMeasure") && r["UnitOfMeasure"] != DBNull.Value)
                p.UnitOfMeasure = r["UnitOfMeasure"].ToString();
            if (HasCol(r, "PackSize") && r["PackSize"] != DBNull.Value)
            {
                decimal pack = Convert.ToDecimal(r["PackSize"]);
                p.PackSize = pack <= 0 ? 1m : pack;
            }
            return p;
        }

        public List<Product> GetAllForManage()
        {
            var list = new List<Product>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(Base() + " ORDER BY p.ProductName", conn))
                using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(Map(r));
            }
            return list;
        }

        public List<Product> SearchAll(string keyword)
        {
            var list = new List<Product>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(Base() +
                    " WHERE (p.ProductCode LIKE @kw OR p.Barcode LIKE @kw OR p.ProductName LIKE @kw) ORDER BY p.ProductName", conn))
                {
                    cmd.Parameters.AddWithValue("@kw", "%" + keyword + "%");
                    using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(Map(r));
                }
            }
            return list;
        }

        public void Update(int id, string name, decimal pur, decimal sale, int minStock, bool active)
        {
            UpdateFull(id, name, pur, sale, minStock, active, null, 1m);
        }

        public void UpdateFull(int id, string name, decimal pur, decimal sale, int minStock, bool active, string uom, decimal packSize)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                try
                {
                    using (var cmd = DbHelper.CreateCommand(
                        "UPDATE Products SET ProductName=@N, PurchasePrice=@P, SalePrice=@S, MinimumStock=@M, IsActive=@A, UnitOfMeasure=@U, PackSize=@Pk WHERE ProductID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@N", name);
                        cmd.Parameters.AddWithValue("@P", pur);
                        cmd.Parameters.AddWithValue("@S", sale);
                        cmd.Parameters.AddWithValue("@M", minStock);
                        cmd.Parameters.AddWithValue("@A", active);
                        cmd.Parameters.AddWithValue("@U", string.IsNullOrEmpty(uom) ? (object)DBNull.Value : uom);
                        cmd.Parameters.AddWithValue("@Pk", packSize <= 0 ? 1m : packSize);
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (SqlException)
                {
                    using (var cmd = DbHelper.CreateCommand(
                        "UPDATE Products SET ProductName=@N, PurchasePrice=@P, SalePrice=@S, MinimumStock=@M, IsActive=@A WHERE ProductID=@ID", conn))
                    {
                        cmd.Parameters.AddWithValue("@N", name);
                        cmd.Parameters.AddWithValue("@P", pur);
                        cmd.Parameters.AddWithValue("@S", sale);
                        cmd.Parameters.AddWithValue("@M", minStock);
                        cmd.Parameters.AddWithValue("@A", active);
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void SetActive(int id, bool active)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("UPDATE Products SET IsActive=@A WHERE ProductID=@ID", conn))
                {
                    cmd.Parameters.AddWithValue("@A", active);
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
