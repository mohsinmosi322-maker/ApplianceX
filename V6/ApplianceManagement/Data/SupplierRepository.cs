using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Data
{
    public class SupplierRepository
    {
        public Supplier GetById(int id)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT * FROM Suppliers WHERE SupplierID=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return Map(r);
                }
            }
            return null;
        }

        public List<Supplier> GetAllActive()
        {
            var list = new List<Supplier>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT * FROM Suppliers WHERE IsActive=1 ORDER BY SupplierName", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(Map(r));
            }
            return list;
        }

        public int CountActive()
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT COUNT(1) FROM Suppliers WHERE IsActive=1", conn))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int Insert(Supplier s)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO Suppliers(SupplierName,Phone,Address,OpeningBalance,IsActive) VALUES(@N,@P,@A,@O,1); SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@N", s.SupplierName);
                    cmd.Parameters.AddWithValue("@P", (object)s.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@A", (object)s.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@O", s.OpeningBalance);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void Update(Supplier s)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "UPDATE Suppliers SET SupplierName=@N, Phone=@P, Address=@A WHERE SupplierID=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", s.SupplierID);
                    cmd.Parameters.AddWithValue("@N", s.SupplierName);
                    cmd.Parameters.AddWithValue("@P", (object)s.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@A", (object)s.Address ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void SetActive(int id, bool active)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "UPDATE Suppliers SET IsActive=@A WHERE SupplierID=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@A", active);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static Supplier Map(SqlDataReader r)
        {
            return new Supplier
            {
                SupplierID = (int)r["SupplierID"],
                SupplierName = r["SupplierName"].ToString(),
                Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                Address = r["Address"] == DBNull.Value ? null : r["Address"].ToString(),
                OpeningBalance = r["OpeningBalance"] == DBNull.Value ? 0 : Convert.ToDecimal(r["OpeningBalance"]),
                IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]),
                CreatedDate = r["CreatedDate"] == DBNull.Value ? DateTime.MinValue : (DateTime)r["CreatedDate"]
            };
        }
    }
}
