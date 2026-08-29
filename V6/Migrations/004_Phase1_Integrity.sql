/*
  Phase 1 integrity — safe to re-run.
  - PackSize / UOM (if missing)
  - Unique barcode (non-null)
  - Check constraints where possible
  - Sync CurrentStock from ledger
  - Optional audit columns on headers
*/
USE APPLIANCE_DB;
GO
SET NOCOUNT ON;

-- Pack / UOM
IF COL_LENGTH('dbo.Products', 'UnitOfMeasure') IS NULL
  ALTER TABLE dbo.Products ADD UnitOfMeasure NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.Products', 'PackSize') IS NULL
  ALTER TABLE dbo.Products ADD PackSize DECIMAL(18,4) NOT NULL CONSTRAINT DF_Products_PackSize DEFAULT (1);
GO
UPDATE dbo.Products SET PackSize = 1 WHERE PackSize IS NULL OR PackSize <= 0;
GO

-- Unique barcode for non-empty values (SQL Server filtered index)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Products_Barcode' AND object_id = OBJECT_ID('dbo.Products'))
BEGIN
  BEGIN TRY
    CREATE UNIQUE INDEX UX_Products_Barcode ON dbo.Products(Barcode) WHERE Barcode IS NOT NULL AND Barcode <> N'';
  END TRY
  BEGIN CATCH
    PRINT 'UX_Products_Barcode skipped: ' + ERROR_MESSAGE();
  END CATCH
END
GO

-- Price / qty sanity (add only if not exists)
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Products_PackSize')
  ALTER TABLE dbo.Products ADD CONSTRAINT CK_Products_PackSize CHECK (PackSize > 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Products_Prices')
  ALTER TABLE dbo.Products ADD CONSTRAINT CK_Products_Prices CHECK (PurchasePrice >= 0 AND SalePrice >= 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_SaleHeader_Amounts')
  ALTER TABLE dbo.SaleHeader WITH NOCHECK ADD CONSTRAINT CK_SaleHeader_Amounts
    CHECK (TotalAmount >= 0 AND Discount >= 0 AND NetAmount >= 0 AND PaidAmount >= 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PurchaseHeader_Amounts')
  ALTER TABLE dbo.PurchaseHeader WITH NOCHECK ADD CONSTRAINT CK_PurchaseHeader_Amounts
    CHECK (TotalAmount >= 0 AND Discount >= 0 AND NetAmount >= 0 AND PaidAmount >= 0);
GO

-- Audit columns on sale/purchase headers (optional)
IF COL_LENGTH('dbo.SaleHeader', 'CreatedBy') IS NULL
  ALTER TABLE dbo.SaleHeader ADD CreatedBy INT NULL;
GO
IF COL_LENGTH('dbo.PurchaseHeader', 'CreatedBy') IS NULL
  ALTER TABLE dbo.PurchaseHeader ADD CreatedBy INT NULL;
GO

-- Supplier invoice number (purchase)
IF COL_LENGTH('dbo.PurchaseHeader', 'SupplierInvoiceNo') IS NULL
  ALTER TABLE dbo.PurchaseHeader ADD SupplierInvoiceNo NVARCHAR(50) NULL;
GO

-- Payment method (sale)
IF COL_LENGTH('dbo.SaleHeader', 'PaymentMethod') IS NULL
  ALTER TABLE dbo.SaleHeader ADD PaymentMethod NVARCHAR(30) NULL;
GO

-- Sync CurrentStock cache from ledger
IF COL_LENGTH('dbo.Products', 'CurrentStock') IS NOT NULL
BEGIN
  UPDATE p SET CurrentStock = ISNULL(x.Bal, 0)
  FROM dbo.Products p
  OUTER APPLY (
    SELECT SUM(QuantityIn) - SUM(QuantityOut) AS Bal
    FROM dbo.InventoryTransaction t
    WHERE t.ProductID = p.ProductID
  ) x;
  PRINT 'CurrentStock synced from InventoryTransaction.';
END
GO

PRINT '004_Phase1_Integrity complete.';
GO
