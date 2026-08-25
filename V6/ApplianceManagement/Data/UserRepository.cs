using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Data
{
    public class UserRepository
    {
        public User Authenticate(string userName, string password)
        {
            string hash = ComputeSha256Hash(password);
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT UserID,UserName,FullName,Role,IsActive,CreatedDate FROM Users WHERE UserName=@U AND PasswordHash=@H AND IsActive=1", conn))
                {
                    cmd.Parameters.AddWithValue("@U", userName);
                    cmd.Parameters.AddWithValue("@H", hash);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                            return new User { UserID=(int)r["UserID"], UserName=r["UserName"].ToString(), FullName=r["FullName"].ToString(), Role=r["Role"].ToString(), IsActive=(bool)r["IsActive"], CreatedDate=(DateTime)r["CreatedDate"] };
                    }
                }
            }
            return null;
        }
        public List<User> GetAll()
        {
            var list = new List<User>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT UserID,UserName,FullName,Role,IsActive,CreatedDate FROM Users ORDER BY UserName", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new User { UserID=(int)r["UserID"], UserName=r["UserName"].ToString(), FullName=r["FullName"].ToString(), Role=r["Role"].ToString(), IsActive=(bool)r["IsActive"], CreatedDate=(DateTime)r["CreatedDate"] });
            }
            return list;
        }
        public bool ExistsUserName(string u)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT COUNT(1) FROM Users WHERE UserName=@U", conn))
                { cmd.Parameters.AddWithValue("@U", u); return Convert.ToInt32(cmd.ExecuteScalar()) > 0; }
            }
        }
        public int Insert(User user, string plainPassword)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("INSERT INTO Users(UserName,PasswordHash,FullName,Role,IsActive) VALUES(@U,@H,@F,@R,1); SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@U", user.UserName);
                    cmd.Parameters.AddWithValue("@H", ComputeSha256Hash(plainPassword));
                    cmd.Parameters.AddWithValue("@F", user.FullName);
                    cmd.Parameters.AddWithValue("@R", user.Role);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }
        public static string ComputeSha256Hash(string raw)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString().ToUpper();
            }
        }
    }
}
