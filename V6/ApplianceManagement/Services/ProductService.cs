using System;
using System.Data.SqlClient;
using ApplianceManagement.Data;
using ApplianceManagement.Helpers;
using ApplianceManagement.Models;

namespace ApplianceManagement.Services
{
    public class ProductService
    {
        private readonly ProductRepository _repo = new ProductRepository();

        public string NextCode()
        {
            // Plain numbers: 1, 2, 3… (not 001)
            using (var conn = DbHelper.GetConnection())
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = DbHelper.CreateCommand(
                            "IF NOT EXISTS (SELECT 1 FROM Settings WITH (UPDLOCK, HOLDLOCK) WHERE SettingName='NextProductCode') " +
                            "INSERT INTO Settings(SettingName,SettingValue) VALUES('NextProductCode','1'); " +
                            "UPDATE Settings WITH (UPDLOCK) SET SettingValue = CAST(CAST(ISNULL(NULLIF(SettingValue,''),'0') AS INT)+1 AS NVARCHAR(50)) WHERE SettingName='NextProductCode'; " +
                            "SELECT CAST(SettingValue AS INT)-1 FROM Settings WHERE SettingName='NextProductCode';", conn, trans))
                        {
                            int n = Convert.ToInt32(cmd.ExecuteScalar());
                            trans.Commit();
                            return n.ToString();
                        }
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        return _repo.GetNextProductCode();
                    }
                }
            }
        }

        public void Validate(Product p, bool isNew)
        {
            if (p == null) throw new ArgumentNullException("p");
            if (string.IsNullOrWhiteSpace(p.ProductName))
                throw new InvalidOperationException("Product name is required.");
            if (string.IsNullOrWhiteSpace(p.ProductCode))
                throw new InvalidOperationException("Product code is required.");
            if (p.PurchasePrice < 0 || p.SalePrice < 0)
                throw new InvalidOperationException("Prices cannot be negative.");
            if (p.PackSize <= 0) p.PackSize = 1;
            if (isNew && _repo.ExistsCode(p.ProductCode.Trim()))
                throw new InvalidOperationException("Product code already exists: " + p.ProductCode);
            if (!string.IsNullOrWhiteSpace(p.Barcode) && isNew && _repo.ExistsBarcode(p.Barcode.Trim()))
                throw new InvalidOperationException("Barcode already exists: " + p.Barcode);
        }

        public int Create(Product p)
        {
            Validate(p, true);
            return _repo.Insert(p);
        }

        public void Update(int id, string name, decimal pur, decimal sale, int min, bool active, string uom, decimal pack)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new InvalidOperationException("Name required.");
            if (pur < 0 || sale < 0) throw new InvalidOperationException("Prices cannot be negative.");
            if (pack <= 0) pack = 1;
            _repo.UpdateFull(id, name, pur, sale, min, active, uom, pack);
        }

        public void Deactivate(int id)
        {
            _repo.SetActive(id, false);
        }
    }
}
