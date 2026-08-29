USE APPLIANCE_DB;
GO
IF COL_LENGTH('dbo.Products', 'UnitOfMeasure') IS NULL
  ALTER TABLE dbo.Products ADD UnitOfMeasure NVARCHAR(30) NULL;
GO
IF COL_LENGTH('dbo.Products', 'PackSize') IS NULL
  ALTER TABLE dbo.Products ADD PackSize DECIMAL(18,4) NOT NULL CONSTRAINT DF_Products_PackSize DEFAULT (1);
GO
PRINT 'UOM / PackSize columns ready.';
