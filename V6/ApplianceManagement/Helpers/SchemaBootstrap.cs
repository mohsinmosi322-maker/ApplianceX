using System;
using System.Data.SqlClient;

namespace ApplianceManagement.Helpers
{
    /// <summary>
    /// Ensures return tables match code expectations and migrates older schemas.
    /// database.sql used ReturnID; code uses SaleReturnID + OriginalSaleDetailID.
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

                        // Create if missing (canonical schema)
                        Exec(conn,
                            "IF OBJECT_ID(N'dbo.SaleReturnHeader', N'U') IS NULL " +
                            "CREATE TABLE dbo.SaleReturnHeader (" +
                            "SaleReturnID INT IDENTITY(1,1) PRIMARY KEY, " +
                            "ReturnNo NVARCHAR(40) NOT NULL UNIQUE, " +
                            "ReturnDate DATETIME NOT NULL CONSTRAINT DF_SRH_Date DEFAULT (GETDATE()), " +
                            "OriginalSaleID INT NULL, " +
                            "CustomerID INT NULL, " +
                            "TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0, " +
                            "Discount DECIMAL(18,2) NOT NULL DEFAULT 0, " +
                            "NetAmount DECIMAL(18,2) NOT NULL DEFAULT 0, " +
                            "RefundAmount DECIMAL(18,2) NOT NULL DEFAULT 0, " +
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
                            "SalePrice DECIMAL(18,4) NOT NULL, " +
                            "Amount DECIMAL(18,2) NOT NULL)");

                        // Migrate old database.sql names: ReturnID → SaleReturnID
                        Exec(conn,
                            "IF COL_LENGTH('dbo.SaleReturnHeader','SaleReturnID') IS NULL AND COL_LENGTH('dbo.SaleReturnHeader','ReturnID') IS NOT NULL " +
                            "EXEC sp_rename 'dbo.SaleReturnHeader.ReturnID', 'SaleReturnID', 'COLUMN'");

                        Exec(conn,
                            "IF COL_LENGTH('dbo.SaleReturnDetail','SaleReturnID') IS NULL AND COL_LENGTH('dbo.SaleReturnDetail','ReturnID') IS NOT NULL " +
                            "EXEC sp_rename 'dbo.SaleReturnDetail.ReturnID', 'SaleReturnID', 'COLUMN'");

                        Exec(conn,
                            "IF COL_LENGTH('dbo.SaleReturnDetail','SaleReturnDetailID') IS NULL AND COL_LENGTH('dbo.SaleReturnDetail','ReturnDetailID') IS NOT NULL " +
                            "EXEC sp_rename 'dbo.SaleReturnDetail.ReturnDetailID', 'SaleReturnDetailID', 'COLUMN'");

                        // Add missing columns on older tables
                        Exec(conn,
                            "IF COL_LENGTH('dbo.SaleReturnHeader','RefundAmount') IS NULL " +
                            "ALTER TABLE dbo.SaleReturnHeader ADD RefundAmount DECIMAL(18,2) NOT NULL DEFAULT 0");
                        Exec(conn,
                            "IF COL_LENGTH('dbo.SaleReturnHeader','OriginalSaleID') IS NULL " +
                            "ALTER TABLE dbo.SaleReturnHeader ADD OriginalSaleID INT NULL");
                        Exec(conn,
                            "IF COL_LENGTH('dbo.SaleReturnHeader','CreatedBy') IS NULL " +
                            "ALTER TABLE dbo.SaleReturnHeader ADD CreatedBy INT NULL");
                        Exec(conn,
                            "IF COL_LENGTH('dbo.SaleReturnDetail','OriginalSaleDetailID') IS NULL " +
                            "ALTER TABLE dbo.SaleReturnDetail ADD OriginalSaleDetailID INT NULL");
                    }
                    _saleReturnDone = true;
                }
                catch (Exception ex)
                {
                    AppLog.Error("EnsureSaleReturnTables", ex);
                    throw;
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
                            "ReturnDate DATETIME NOT NULL DEFAULT GETDATE(), " +
                            "OriginalPurchaseID INT NULL, " +
                            "SupplierID INT NULL, " +
                            "TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0, " +
                            "Discount DECIMAL(18,2) NOT NULL DEFAULT 0, " +
                            "NetAmount DECIMAL(18,2) NOT NULL DEFAULT 0, " +
                            "RefundAmount DECIMAL(18,2) NOT NULL DEFAULT 0, " +
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
                            "PurchasePrice DECIMAL(18,4) NOT NULL, " +
                            "Amount DECIMAL(18,2) NOT NULL)");
                        Exec(conn,
                            "IF COL_LENGTH('dbo.PurchaseReturnDetail','OriginalPurchaseDetailID') IS NULL " +
                            "ALTER TABLE dbo.PurchaseReturnDetail ADD OriginalPurchaseDetailID INT NULL");
                    }
                    _purchaseReturnDone = true;
                }
                catch (Exception ex)
                {
                    AppLog.Error("EnsurePurchaseReturnTables", ex);
                    throw;
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
