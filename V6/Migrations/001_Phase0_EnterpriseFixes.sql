-- ApplianceX Phase 0 enterprise fixes
-- Run once against APPLIANCE_DB (or your catalog)

SET NOCOUNT ON;
GO

-- 1) Physical stock column (ledger remains source of truth for history)
IF COL_LENGTH('dbo.Products', 'CurrentStock') IS NULL
BEGIN
    ALTER TABLE dbo.Products ADD CurrentStock INT NOT NULL CONSTRAINT DF_Products_CurrentStock DEFAULT (0);
END
GO

-- Backfill from inventory ledger
UPDATE p
SET CurrentStock = ISNULL((
    SELECT SUM(QuantityIn) - SUM(QuantityOut)
    FROM dbo.InventoryTransaction t
    WHERE t.ProductID = p.ProductID
), 0)
FROM dbo.Products p;
GO

-- 2) Snapshot cost on sale lines (for true profit later)
IF COL_LENGTH('dbo.SaleDetail', 'UnitCost') IS NULL
BEGIN
    ALTER TABLE dbo.SaleDetail ADD UnitCost DECIMAL(18,2) NULL;
END
GO

-- 3) Ensure invoice counter settings exist
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'NextInvoiceNumber')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'NextInvoiceNumber', N'1');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'InvoicePrefix')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'InvoicePrefix', N'INV-');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'NextPurchaseInvoiceNumber')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'NextPurchaseInvoiceNumber', N'1');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'PurchaseInvoicePrefix')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'PurchaseInvoicePrefix', N'PUR-');
GO

PRINT 'Phase 0 migration completed.';
