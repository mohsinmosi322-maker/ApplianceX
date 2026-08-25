using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace ApplianceManagement.Helpers
{
    public static class LicenseReader
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

        public static string LicensePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.dat"); }
        }

        public static LicenseInfo Current { get; private set; }

        public static bool TryLoad()
        {
            try
            {
                if (!File.Exists(LicensePath)) { Current = null; return false; }
                string plain = Decrypt(File.ReadAllText(LicensePath).Trim());
                var xml = XElement.Parse(plain);
                Current = new LicenseInfo
                {
                    StoreName = (string)xml.Element("StoreName") ?? "",
                    ShopPhone = (string)xml.Element("ShopPhone") ?? "",
                    InvoicePrefix = (string)xml.Element("InvoicePrefix") ?? "INV-",
                    ExpiryDate = DateTime.Parse((string)xml.Element("ExpiryDate") ?? "2099-12-31", CultureInfo.InvariantCulture),
                    AllowPrint = ((string)xml.Element("AllowPrint") ?? "1") == "1",
                    MaxDiscountAdmin = 0,
                    MaxDiscountUser = 0,
                    ConnectionString = (string)xml.Element("ConnectionString") ?? "",
                    ClientId = (string)xml.Element("ClientId") ?? "",
                    SoftwareName = (string)xml.Element("SoftwareName") ?? "Appliance Management System",
                    VendorContact = (string)xml.Element("VendorContact") ?? "",
                    AppVersion = (string)xml.Element("AppVersion") ?? "2.1.0"
                };
                return true;
            }
            catch { Current = null; return false; }
        }

        public static bool IsValid()
        {
            return Current != null && DateTime.Today <= Current.ExpiryDate.Date;
        }

        private static string Decrypt(string cipher)
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
    }
}
