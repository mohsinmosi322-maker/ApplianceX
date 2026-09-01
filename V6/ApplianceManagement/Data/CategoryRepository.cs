using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Data
{
    public class CategoryRepository
    {
        public List<Category> GetAllActive()
        {
            var list = new List<Category>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT * FROM Categories WHERE IsActive=1 ORDER BY CategoryName", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new Category
                        {
                            CategoryID = (int)r["CategoryID"],
                            CategoryName = r["CategoryName"].ToString(),
                            IsActive = (bool)r["IsActive"]
                        });
            }
            return list;
        }

        public bool ExistsName(string name, int excludeId = 0)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT COUNT(1) FROM Categories WHERE CategoryName=@N AND IsActive=1 AND CategoryID<>@Ex", conn))
                {
                    cmd.Parameters.AddWithValue("@N", name.Trim());
                    cmd.Parameters.AddWithValue("@Ex", excludeId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public int Insert(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Category name is required.");
            if (ExistsName(name))
                throw new InvalidOperationException("Category already exists: " + name.Trim());

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO Categories(CategoryName,IsActive) VALUES(@N,1); SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@N", name.Trim());
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        public void Update(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Category name is required.");
            if (ExistsName(name, id))
                throw new InvalidOperationException("Category already exists: " + name.Trim());

            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "UPDATE Categories SET CategoryName=@N WHERE CategoryID=@ID", conn))
                {
                    cmd.Parameters.AddWithValue("@N", name.Trim());
                    cmd.Parameters.AddWithValue("@ID", id);
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
                    "UPDATE Categories SET IsActive=@A WHERE CategoryID=@ID", conn))
                {
                    cmd.Parameters.AddWithValue("@A", active);
                    cmd.Parameters.AddWithValue("@ID", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
