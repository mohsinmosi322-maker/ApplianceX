USE APPLIANCE_DB;
GO
SET NOCOUNT ON;

-- Purchase Return
IF OBJECT_ID(N'dbo.PurchaseReturnHeader', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.PurchaseReturnHeader (
    PurchaseReturnID INT IDENTITY(1,1) PRIMARY KEY,
    ReturnNo NVARCHAR(40) NOT NULL UNIQUE,
    ReturnDate DATETIME NOT NULL CONSTRAINT DF_PR_Date DEFAULT (GETDATE()),
    OriginalPurchaseID INT NOT NULL,
    SupplierID INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT (0),
    Discount DECIMAL(18,2) NOT NULL DEFAULT (0),
    NetAmount DECIMAL(18,2) NOT NULL DEFAULT (0),
    RefundAmount DECIMAL(18,2) NOT NULL DEFAULT (0),
    Remarks NVARCHAR(300) NULL,
    CreatedBy INT NULL
  );
END
GO
IF OBJECT_ID(N'dbo.PurchaseReturnDetail', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.PurchaseReturnDetail (
    PurchaseReturnDetailID INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseReturnID INT NOT NULL,
    OriginalPurchaseDetailID INT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    PurchasePrice DECIMAL(18,2) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL
  );
END
GO

-- Customer / Supplier ledgers
IF OBJECT_ID(N'dbo.CustomerLedger', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.CustomerLedger (
    LedgerID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    EntryDate DATETIME NOT NULL CONSTRAINT DF_CL_Date DEFAULT (GETDATE()),
    EntryType NVARCHAR(30) NOT NULL, -- OPENING, SALE, PAYMENT, SALE_RETURN
    ReferenceID INT NULL,
    ReferenceNo NVARCHAR(40) NULL,
    Debit DECIMAL(18,2) NOT NULL DEFAULT (0),
    Credit DECIMAL(18,2) NOT NULL DEFAULT (0),
    Remarks NVARCHAR(300) NULL,
    CreatedBy INT NULL
  );
END
GO
IF OBJECT_ID(N'dbo.SupplierLedger', N'U') IS NULL
BEGIN
  CREATE TABLE dbo.SupplierLedger (
    LedgerID INT IDENTITY(1,1) PRIMARY KEY,
    SupplierID INT NOT NULL,
    EntryDate DATETIME NOT NULL CONSTRAINT DF_SL_Date DEFAULT (GETDATE()),
    EntryType NVARCHAR(30) NOT NULL, -- OPENING, PURCHASE, PAYMENT, PURCHASE_RETURN
    ReferenceID INT NULL,
    ReferenceNo NVARCHAR(40) NULL,
    Debit DECIMAL(18,2) NOT NULL DEFAULT (0),
    Credit DECIMAL(18,2) NOT NULL DEFAULT (0),
    Remarks NVARCHAR(300) NULL,
    CreatedBy INT NULL
  );
END
GO

-- Counters
IF NOT EXISTS (SELECT 1 FROM Settings WHERE SettingName=N'PurchaseReturnPrefix')
  INSERT INTO Settings(SettingName,SettingValue) VALUES (N'PurchaseReturnPrefix', N'PR-');
IF NOT EXISTS (SELECT 1 FROM Settings WHERE SettingName=N'NextPurchaseReturnNumber')
  INSERT INTO Settings(SettingName,SettingValue) VALUES (N'NextPurchaseReturnNumber', N'1');
GO

PRINT '006 Accounts / PurchaseReturn / Stock ops schema ready.';
GO
