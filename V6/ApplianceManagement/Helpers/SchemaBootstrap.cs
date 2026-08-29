using System;
using System.Data.SqlClient;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Ensures critical tables exist (for DBs that never ran migrations 005/006).
    /// Safe to call repeatedly.
    /// </summary>
    public static class SchemaBootstrap
    {
        private static bool _saleReturnDone;
        private static bool _purchaseReturnDone;
        private static readonly object _lock = new object();

        public static void EnsureSaleReturnTables()
        {
            if (_saleReturnDone) return;
            lock (_lock)
            {
                if (_saleReturnDone) return;
                try
                {
                    using (var conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        Exec(conn,
                            "IF OBJECT_ID(N'dbo.SaleReturnHeader', N'U') IS NULL " +
                            "CREATE TABLE dbo.SaleReturnHeader (" +
                            "SaleReturnID INT IDENTITY(1,1) PRIMARY KEY, " +
                            "ReturnNo NVARCHAR(40) NOT NULL UNIQUE, " +
                            "ReturnDate DATETIME NOT NULL CONSTRAINT DF_SR_Date DEFAULT (GETDATE()), " +
                            "OriginalSaleID INT NOT NULL, " +
                            "CustomerID INT NOT NULL, " +
                            "TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SR_Tot DEFAULT (0), " +
                            "Discount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SR_Disc DEFAULT (0), " +
                            "NetAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SR_Net DEFAULT (0), " +
                            "RefundAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SR_Ref DEFAULT (0), " +
                            "Remarks NVARCHAR(300) NULL, " +
                            "CreatedBy INT NULL)");
                        Exec(conn,
                            "IF OBJECT_ID(N'dbo.SaleReturnDetail', N'U') IS NULL " +
                            "CREATE TABLE dbo.SaleReturnDetail (" +
                            "SaleReturnDetailID INT IDENTITY(1,1) PRIMARY KEY, " +
                            "SaleReturnID INT NOT NULL, " +
                            "OriginalSaleDetailID INT NULL, " +
                            "ProductID INT NOT NULL, " +
                            "Quantity INT NOT NULL, " +
                            "SalePrice DECIMAL(18,2) NOT NULL, " +
                            "Amount DECIMAL(18,2) NOT NULL)");
                        Exec(conn,
                            "IF NOT EXISTS (SELECT 1 FROM Settings WHERE SettingName=N'SaleReturnPrefix') " +
                            "INSERT INTO Settings(SettingName,SettingValue) VALUES(N'SaleReturnPrefix',N'RET-')");
                        Exec(conn,
                            "IF NOT EXISTS (SELECT 1 FROM Settings WHERE SettingName=N'NextSaleReturnNumber') " +
                            "INSERT INTO Settings(SettingName,SettingValue) VALUES(N'NextSaleReturnNumber',N'1')");
                    }
                    _saleReturnDone = true;
                    AppLog.Info("SchemaBootstrap: SaleReturn tables ready");
                }
                catch (Exception ex)
                {
                    AppLog.Error("SchemaBootstrap SaleReturn", ex);
                    throw new InvalidOperationException(
                        "Sale return tables missing. Run V6/Migrations/005_SaleReturn.sql on APPLIANCE_DB.\n" + ex.Message, ex);
                }
            }
        }

        public static void EnsurePurchaseReturnTables()
        {
            if (_purchaseReturnDone) return;
            lock (_lock)
            {
                if (_purchaseReturnDone) return;
                try
                {
                    using (var conn = DbHelper.GetConnection())
                    {
                        conn.Open();
                        Exec(conn,
                            "IF OBJECT_ID(N'dbo.PurchaseReturnHeader', N'U') IS NULL " +
                            "CREATE TABLE dbo.PurchaseReturnHeader (" +
                            "PurchaseReturnID INT IDENTITY(1,1) PRIMARY KEY, " +
                            "ReturnNo NVARCHAR(40) NOT NULL UNIQUE, " +
                            "ReturnDate DATETIME NOT NULL CONSTRAINT DF_PR_Date DEFAULT (GETDATE()), " +
                            "OriginalPurchaseID INT NOT NULL, " +
                            "SupplierID INT NOT NULL, " +
                            "TotalAmount DECIMAL(18,2) NOT NULL DEFAULT (0), " +
                            "Discount DECIMAL(18,2) NOT NULL DEFAULT (0), " +
                            "NetAmount DECIMAL(18,2) NOT NULL DEFAULT (0), " +
                            "RefundAmount DECIMAL(18,2) NOT NULL DEFAULT (0), " +
                            "Remarks NVARCHAR(300) NULL, " +
                            "CreatedBy INT NULL)");
                        Exec(conn,
                            "IF OBJECT_ID(N'dbo.PurchaseReturnDetail', N'U') IS NULL " +
                            "CREATE TABLE dbo.PurchaseReturnDetail (" +
                            "PurchaseReturnDetailID INT IDENTITY(1,1) PRIMARY KEY, " +
                            "PurchaseReturnID INT NOT NULL, " +
                            "OriginalPurchaseDetailID INT NULL, " +
                            "ProductID INT NOT NULL, " +
                            "Quantity INT NOT NULL, " +
                            "PurchasePrice DECIMAL(18,2) NOT NULL, " +
                            "Amount DECIMAL(18,2) NOT NULL)");
                        Exec(conn,
                            "IF NOT EXISTS (SELECT 1 FROM Settings WHERE SettingName=N'PurchaseReturnPrefix') " +
                            "INSERT INTO Settings(SettingName,SettingValue) VALUES(N'PurchaseReturnPrefix',N'PR-')");
                        Exec(conn,
                            "IF NOT EXISTS (SELECT 1 FROM Settings WHERE SettingName=N'NextPurchaseReturnNumber') " +
                            "INSERT INTO Settings(SettingName,SettingValue) VALUES(N'NextPurchaseReturnNumber',N'1')");
                    }
                    _purchaseReturnDone = true;
                    AppLog.Info("SchemaBootstrap: PurchaseReturn tables ready");
                }
                catch (Exception ex)
                {
                    AppLog.Error("SchemaBootstrap PurchaseReturn", ex);
                    throw new InvalidOperationException(
                        "Purchase return tables missing. Run V6/Migrations/006_Accounts_PurchaseReturn_StockOps.sql.\n" + ex.Message, ex);
                }
            }
        }

        private static void Exec(SqlConnection conn, string sql)
        {
            using (var cmd = DbHelper.CreateCommand(sql, conn))
                cmd.ExecuteNonQuery();
        }
    }
}
