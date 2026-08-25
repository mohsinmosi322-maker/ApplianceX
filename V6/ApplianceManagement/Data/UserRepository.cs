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
        private const int Pbkdf2Iterations = 100000;

        public User Authenticate(string userName, string password)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT UserID,UserName,PasswordHash,FullName,Role,IsActive,CreatedDate FROM Users WHERE UserName=@U AND IsActive=1", conn))
                {
                    cmd.Parameters.AddWithValue("@U", userName);
                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read()) return null;

                        string stored = r["PasswordHash"].ToString();
                        if (!VerifyPassword(password, stored))
                            return null;

                        var user = new User
                        {
                            UserID = (int)r["UserID"],
                            UserName = r["UserName"].ToString(),
                            FullName = r["FullName"].ToString(),
                            Role = r["Role"].ToString(),
                            IsActive = (bool)r["IsActive"],
                            CreatedDate = (DateTime)r["CreatedDate"]
                        };

                        // Upgrade legacy SHA256 hashes to PBKDF2 on successful login
                        bool isLegacy = stored.IndexOf(':') < 0;
                        r.Close();
                        if (isLegacy)
                        {
                            try { UpdatePasswordHash(user.UserID, HashPassword(password)); }
                            catch (Exception ex) { AppLog.Error("Failed to upgrade password hash for " + userName, ex); }
                        }

                        return user;
                    }
                }
            }
        }

        public List<User> GetAll()
        {
            var list = new List<User>();
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "SELECT UserID,UserName,FullName,Role,IsActive,CreatedDate FROM Users ORDER BY UserName", conn))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new User
                        {
                            UserID = (int)r["UserID"],
                            UserName = r["UserName"].ToString(),
                            FullName = r["FullName"].ToString(),
                            Role = r["Role"].ToString(),
                            IsActive = (bool)r["IsActive"],
                            CreatedDate = (DateTime)r["CreatedDate"]
                        });
            }
            return list;
        }

        public bool ExistsUserName(string u)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("SELECT COUNT(1) FROM Users WHERE UserName=@U", conn))
                {
                    cmd.Parameters.AddWithValue("@U", u);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public int Insert(User user, string plainPassword)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand(
                    "INSERT INTO Users(UserName,PasswordHash,FullName,Role,IsActive) VALUES(@U,@H,@F,@R,1); SELECT SCOPE_IDENTITY();", conn))
                {
                    cmd.Parameters.AddWithValue("@U", user.UserName);
                    cmd.Parameters.AddWithValue("@H", HashPassword(plainPassword));
                    cmd.Parameters.AddWithValue("@F", user.FullName);
                    cmd.Parameters.AddWithValue("@R", user.Role);
                    return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
        }

        private void UpdatePasswordHash(int userId, string hash)
        {
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var cmd = DbHelper.CreateCommand("UPDATE Users SET PasswordHash=@H WHERE UserID=@ID", conn))
                {
                    cmd.Parameters.AddWithValue("@H", hash);
                    cmd.Parameters.AddWithValue("@ID", userId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>PBKDF2 format: iterations:saltBase64:hashBase64</summary>
        public static string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return Pbkdf2Iterations + ":" +
                       Convert.ToBase64String(salt) + ":" +
                       Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;

            // New format
            if (stored.Contains(":"))
            {
                var parts = stored.Split(':');
                if (parts.Length != 3) return false;
                if (!int.TryParse(parts[0], out int iterations)) return false;
                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] expected = Convert.FromBase64String(parts[2]);

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                {
                    byte[] actual = pbkdf2.GetBytes(expected.Length);
                    return FixedTimeEquals(actual, expected);
                }
            }

            // Legacy SHA256 (uppercase hex) — still accepted, upgraded on login
            string legacy = ComputeSha256HashLegacy(password);
            return string.Equals(legacy, stored, StringComparison.OrdinalIgnoreCase);
        }

        public static string ComputeSha256Hash(string raw) => HashPassword(raw);

        private static string ComputeSha256HashLegacy(string raw)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString().ToUpper();
            }
        }

        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
