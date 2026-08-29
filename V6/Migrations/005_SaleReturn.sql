/*
  Sale Return linked to original invoice.
  Safe to re-run. REQUIRED before using Sale Return form.
*/
USE APPLIANCE_DB;
GO
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.SaleReturnHeader', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.SaleReturnHeader (
    SaleReturnID   INT IDENTITY(1,1) PRIMARY KEY,
    ReturnNo       NVARCHAR(40) NOT NULL UNIQUE,
    ReturnDate     DATETIME NOT NULL CONSTRAINT DF_SR_Date DEFAULT (GETDATE()),
    OriginalSaleID INT NOT NULL,
    CustomerID     INT NOT NULL,
    TotalAmount    DECIMAL(18,2) NOT NULL CONSTRAINT DF_SR_Tot DEFAULT (0),
    Discount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_SR_Disc DEFAULT (0),
    NetAmount      DECIMAL(18,2) NOT NULL CONSTRAINT DF_SR_Net DEFAULT (0),
    RefundAmount   DECIMAL(18,2) NOT NULL CONSTRAINT DF_SR_Ref DEFAULT (0),
    Remarks        NVARCHAR(300) NULL,
    CreatedBy      INT NULL
  );
END
GO

IF OBJECT_ID(N'dbo.SaleReturnDetail', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.SaleReturnDetail (
    SaleReturnDetailID   INT IDENTITY(1,1) PRIMARY KEY,
    SaleReturnID         INT NOT NULL,
    OriginalSaleDetailID INT NULL,
    ProductID            INT NOT NULL,
    Quantity             INT NOT NULL,
    SalePrice            DECIMAL(18,2) NOT NULL,
    Amount               DECIMAL(18,2) NOT NULL
  );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'SaleReturnPrefix')
  INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'SaleReturnPrefix', N'RET-');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'NextSaleReturnNumber')
  INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'NextSaleReturnNumber', N'1');
GO

PRINT '005_SaleReturn ready — SaleReturnHeader + SaleReturnDetail created.';
GO
