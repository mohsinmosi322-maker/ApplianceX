USE APPLIANCE_DB;
GO
IF COL_LENGTH('dbo.Products', 'UnitOfMeasure') IS NULL
  ALTER TABLE dbo.Products ADD UnitOfMeasure NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.Products', 'PackSize') IS NULL
  ALTER TABLE dbo.Products ADD PackSize DECIMAL(18,4) NOT NULL CONSTRAINT DF_Products_PackSize DEFAULT (1);
GO
-- Ensure existing rows have PackSize = 1 if somehow null (SQL Server NOT NULL + DEFAULT handles new rows)
UPDATE dbo.Products SET PackSize = 1 WHERE PackSize IS NULL OR PackSize <= 0;
GO
PRINT 'UOM / PackSize columns ready. Re-save products with pack size if needed.';
