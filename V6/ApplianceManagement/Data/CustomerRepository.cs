using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Data
{
    public class CustomerRepository
    {
        public Customer GetWalkInCustomer()
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT * FROM Customers WHERE CustomerName='Walk-in Customer' AND IsActive=1", conn))
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read()) return Map(r);
                }
            }
            return null;
        }

        public Customer GetById(int id)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT * FROM Customers WHERE CustomerID=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var r = cmd.ExecuteReader())
                        if (r.Read()) return Map(r);
                }
            }
            return null;
        }

        public List<Customer> GetAllActive()
        {
            var list = new List<Customer>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT * FROM Customers WHERE IsActive=1 ORDER BY CustomerName", conn))
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
                using (var cmd = DbHelper.CreateCommand("SELECT COUNT(1) FROM Customers WHERE IsActive=1", conn))
                    return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int Insert(Customer c)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO Customers(CustomerName,Phone,Address,OpeningBalance,IsActive) VALUES(@N,@P,@A,@O,1); SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@N", c.CustomerName);
                    cmd.Parameters.AddWithValue("@P", (object)c.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@A", (object)c.Address ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@O", c.OpeningBalance);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void Update(Customer c)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "UPDATE Customers SET CustomerName=@N, Phone=@P, Address=@A WHERE CustomerID=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", c.CustomerID);
                    cmd.Parameters.AddWithValue("@N", c.CustomerName);
                    cmd.Parameters.AddWithValue("@P", (object)c.Phone ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@A", (object)c.Address ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>Soft-delete. Walk-in customer cannot be deactivated.</summary>
        public void SetActive(int id, bool active)
        {
            var c = GetById(id);
            if (c != null && string.Equals(c.CustomerName, "Walk-in Customer", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Walk-in Customer cannot be deactivated.");

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "UPDATE Customers SET IsActive=@A WHERE CustomerID=@Id", conn))
                {
                    cmd.Parameters.AddWithValue("@A", active);
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static Customer Map(SqlDataReader r)
        {
            return new Customer
            {
                CustomerID = (int)r["CustomerID"],
                CustomerName = r["CustomerName"].ToString(),
                Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                Address = r["Address"] == DBNull.Value ? null : r["Address"].ToString(),
                OpeningBalance = r["OpeningBalance"] == DBNull.Value ? 0 : Convert.ToDecimal(r["OpeningBalance"]),
                IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"])
            };
        }
    }
}
