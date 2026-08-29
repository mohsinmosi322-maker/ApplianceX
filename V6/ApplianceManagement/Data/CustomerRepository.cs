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

        private static Customer Map(SqlDataReader r)
        {
            return new Customer
            {
                CustomerID = (int)r["CustomerID"],
                CustomerName = r["CustomerName"].ToString(),
                Phone = r["Phone"] == DBNull.Value ? null : r["Phone"].ToString(),
                Address = r["Address"] == DBNull.Value ? null : r["Address"].ToString(),
                OpeningBalance = (decimal)r["OpeningBalance"],
                IsActive = (bool)r["IsActive"],
                CreatedDate = (DateTime)r["CreatedDate"]
            };
        }
    }
}
