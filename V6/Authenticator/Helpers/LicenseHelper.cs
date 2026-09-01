using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Authenticator.Helpers
{
    public static class LicenseHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("AppLicKey16Bytes");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("AppLicIV16Bytes!");

        public class LicenseInfo
        {
            public string StoreName { get; set; }
            public string ShopPhone { get; set; }
            public string InvoicePrefix { get; set; }
            public DateTime ExpiryDate { get; set; }
            public bool AllowPrint { get; set; }
            public decimal MaxDiscountAdmin { get; set; }
            public decimal MaxDiscountUser { get; set; }
            public string ConnectionString { get; set; }
            public string ClientId { get; set; }
            public string SoftwareName { get; set; }
            public string VendorContact { get; set; }
            public string AppVersion { get; set; }
        }

        public static string Encrypt(string plain)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key; aes.IV = IV;
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] data = Encoding.UTF8.GetBytes(plain);
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipher)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key; aes.IV = IV;
                byte[] data = Convert.FromBase64String(cipher);
                using (var ms = new MemoryStream(data))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs))
                    return sr.ReadToEnd();
            }
        }

        public static void SaveLicense(string filePath, LicenseInfo info)
        {
            // InvoicePrefix intentionally empty — app uses plain 1,2,3… invoice numbers
            var xml = new XElement("License",
                new XElement("Version", "2"),
                new XElement("StoreName", info.StoreName ?? ""),
                new XElement("ShopPhone", info.ShopPhone ?? ""),
                new XElement("InvoicePrefix", ""),
                new XElement("ExpiryDate", info.ExpiryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                new XElement("AllowPrint", info.AllowPrint ? "1" : "0"),
                new XElement("MaxDiscountAdmin", "0"),
                new XElement("MaxDiscountUser", "0"),
                new XElement("ConnectionString", info.ConnectionString ?? ""),
                new XElement("ClientId", info.ClientId ?? Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()),
                new XElement("SoftwareName", info.SoftwareName ?? "Appliance Management System"),
                new XElement("VendorContact", info.VendorContact ?? ""),
                new XElement("AppVersion", info.AppVersion ?? "2.1.0")
            );
            File.WriteAllText(filePath, Encrypt(xml.ToString(SaveOptions.DisableFormatting)));
        }

        public static LicenseInfo LoadLicense(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            string plain = Decrypt(File.ReadAllText(filePath).Trim());
            var xml = XElement.Parse(plain);
            return new LicenseInfo
            {
                StoreName = (string)xml.Element("StoreName") ?? "",
                ShopPhone = (string)xml.Element("ShopPhone") ?? "",
                InvoicePrefix = "",
                ExpiryDate = DateTime.Parse((string)xml.Element("ExpiryDate") ?? "2099-12-31", CultureInfo.InvariantCulture),
                AllowPrint = ((string)xml.Element("AllowPrint") ?? "1") == "1",
                ConnectionString = (string)xml.Element("ConnectionString") ?? "",
                ClientId = (string)xml.Element("ClientId") ?? "",
                SoftwareName = (string)xml.Element("SoftwareName") ?? "Appliance Management System",
                VendorContact = (string)xml.Element("VendorContact") ?? "",
                AppVersion = (string)xml.Element("AppVersion") ?? "2.1.0"
            };
        }

        public static bool IsExpired(LicenseInfo info)
        {
            return info == null || DateTime.Today > info.ExpiryDate.Date;
        }
    }
}
