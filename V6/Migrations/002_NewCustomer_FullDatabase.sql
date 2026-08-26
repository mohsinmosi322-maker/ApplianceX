/*
  ApplianceX - FULL DATABASE for a NEW CUSTOMER
  ---------------------------------------------
  SSMS mein run karein (Windows auth ya SQL auth).
  Database name change kar sakte ho: APPLIANCE_DB

  Default login after install:
    Username : admin
    Password : admin123
  (pehli login ke baad password change karein)
*/

SET NOCOUNT ON;
GO

IF DB_ID(N'APPLIANCE_DB') IS NULL
BEGIN
    CREATE DATABASE APPLIANCE_DB;
END
GO

USE APPLIANCE_DB;
GO

-- ===== Users =====
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserID       INT IDENTITY(1,1) PRIMARY KEY,
        UserName     NVARCHAR(50)  NOT NULL UNIQUE,
        PasswordHash NVARCHAR(200) NOT NULL,
        FullName     NVARCHAR(100) NOT NULL,
        Role         NVARCHAR(30)  NOT NULL CONSTRAINT DF_Users_Role DEFAULT (N'User'),
        IsActive     BIT           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedDate  DATETIME      NOT NULL CONSTRAINT DF_Users_Created DEFAULT (GETDATE())
    );
END
GO

-- ===== Categories =====
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories (
        CategoryID   INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName NVARCHAR(100) NOT NULL,
        IsActive     BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1)
    );
END
GO

-- ===== Products =====
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        ProductID     INT IDENTITY(1,1) PRIMARY KEY,
        ProductCode   NVARCHAR(50)  NOT NULL UNIQUE,
        Barcode       NVARCHAR(50)  NULL,
        ProductName   NVARCHAR(200) NOT NULL,
        CategoryID    INT NOT NULL REFERENCES dbo.Categories(CategoryID),
        PurchasePrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_Purchase DEFAULT (0),
        SalePrice     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_Sale DEFAULT (0),
        MinimumStock  INT NOT NULL CONSTRAINT DF_Products_MinStock DEFAULT (0),
        CurrentStock  INT NOT NULL CONSTRAINT DF_Products_CurrentStock DEFAULT (0),
        IsActive      BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CreatedDate   DATETIME NOT NULL CONSTRAINT DF_Products_Created DEFAULT (GETDATE())
    );
END
GO

-- ===== Customers =====
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers (
        CustomerID     INT IDENTITY(1,1) PRIMARY KEY,
        CustomerName   NVARCHAR(150) NOT NULL,
        Phone          NVARCHAR(30)  NULL,
        Address        NVARCHAR(300) NULL,
        OpeningBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Customers_OB DEFAULT (0),
        IsActive       BIT NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1),
        CreatedDate    DATETIME NOT NULL CONSTRAINT DF_Customers_Created DEFAULT (GETDATE())
    );
END
GO

-- ===== Suppliers =====
IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Suppliers (
        SupplierID     INT IDENTITY(1,1) PRIMARY KEY,
        SupplierName   NVARCHAR(150) NOT NULL,
        Phone          NVARCHAR(30)  NULL,
        Address        NVARCHAR(300) NULL,
        OpeningBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Suppliers_OB DEFAULT (0),
        IsActive       BIT NOT NULL CONSTRAINT DF_Suppliers_IsActive DEFAULT (1),
        CreatedDate    DATETIME NOT NULL CONSTRAINT DF_Suppliers_Created DEFAULT (GETDATE())
    );
END
GO

-- ===== Sale =====
IF OBJECT_ID(N'dbo.SaleHeader', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SaleHeader (
        SaleID         INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNo      NVARCHAR(40) NOT NULL UNIQUE,
        SaleDate       DATETIME NOT NULL CONSTRAINT DF_Sale_Date DEFAULT (GETDATE()),
        CustomerID     INT NOT NULL REFERENCES dbo.Customers(CustomerID),
        TotalAmount    DECIMAL(18,2) NOT NULL,
        Discount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sale_Disc DEFAULT (0),
        NetAmount      DECIMAL(18,2) NOT NULL,
        PaidAmount     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sale_Paid DEFAULT (0),
        BalanceAmount  DECIMAL(18,2) NOT NULL CONSTRAINT DF_Sale_Bal DEFAULT (0),
        Remarks        NVARCHAR(300) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.SaleDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SaleDetail (
        SaleDetailID INT IDENTITY(1,1) PRIMARY KEY,
        SaleID       INT NOT NULL REFERENCES dbo.SaleHeader(SaleID),
        ProductID    INT NOT NULL REFERENCES dbo.Products(ProductID),
        Quantity     INT NOT NULL,
        SalePrice    DECIMAL(18,2) NOT NULL,
        Discount     DECIMAL(18,2) NOT NULL CONSTRAINT DF_SaleDet_Disc DEFAULT (0),
        Amount       DECIMAL(18,2) NOT NULL,
        UnitCost     DECIMAL(18,2) NULL
    );
END
GO

-- ===== Purchase =====
IF OBJECT_ID(N'dbo.PurchaseHeader', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseHeader (
        PurchaseID     INT IDENTITY(1,1) PRIMARY KEY,
        InvoiceNo      NVARCHAR(40) NOT NULL UNIQUE,
        PurchaseDate   DATETIME NOT NULL CONSTRAINT DF_Pur_Date DEFAULT (GETDATE()),
        SupplierID     INT NOT NULL REFERENCES dbo.Suppliers(SupplierID),
        TotalAmount    DECIMAL(18,2) NOT NULL,
        Discount       DECIMAL(18,2) NOT NULL CONSTRAINT DF_Pur_Disc DEFAULT (0),
        NetAmount      DECIMAL(18,2) NOT NULL,
        PaidAmount     DECIMAL(18,2) NOT NULL CONSTRAINT DF_Pur_Paid DEFAULT (0),
        BalanceAmount  DECIMAL(18,2) NOT NULL CONSTRAINT DF_Pur_Bal DEFAULT (0),
        Remarks        NVARCHAR(300) NULL
    );
END
GO

IF OBJECT_ID(N'dbo.PurchaseDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseDetail (
        PurchaseDetailID INT IDENTITY(1,1) PRIMARY KEY,
        PurchaseID       INT NOT NULL REFERENCES dbo.PurchaseHeader(PurchaseID),
        ProductID        INT NOT NULL REFERENCES dbo.Products(ProductID),
        Quantity         INT NOT NULL,
        PurchasePrice    DECIMAL(18,2) NOT NULL,
        Discount         DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurDet_Disc DEFAULT (0),
        Amount           DECIMAL(18,2) NOT NULL
    );
END
GO

-- ===== Inventory ledger =====
IF OBJECT_ID(N'dbo.InventoryTransaction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryTransaction (
        TransactionID   INT IDENTITY(1,1) PRIMARY KEY,
        ProductID       INT NOT NULL REFERENCES dbo.Products(ProductID),
        TransactionDate DATETIME NOT NULL CONSTRAINT DF_Inv_Date DEFAULT (GETDATE()),
        TransactionType NVARCHAR(20) NOT NULL, -- SALE / PURCHASE / ADJUST
        ReferenceNo     NVARCHAR(40) NULL,
        QuantityIn      INT NOT NULL CONSTRAINT DF_Inv_In DEFAULT (0),
        QuantityOut     INT NOT NULL CONSTRAINT DF_Inv_Out DEFAULT (0),
        Remarks         NVARCHAR(200) NULL
    );
END
GO

-- ===== Settings =====
IF OBJECT_ID(N'dbo.Settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Settings (
        SettingID    INT IDENTITY(1,1) PRIMARY KEY,
        SettingName  NVARCHAR(100) NOT NULL UNIQUE,
        SettingValue NVARCHAR(500) NULL
    );
END
GO

-- ===== Seed data =====
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
    INSERT INTO dbo.Categories(CategoryName) VALUES (N'General');

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerName = N'Walk-in Customer')
    INSERT INTO dbo.Customers(CustomerName, Phone, OpeningBalance, IsActive)
    VALUES (N'Walk-in Customer', NULL, 0, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Suppliers WHERE SupplierName = N'Default Supplier')
    INSERT INTO dbo.Suppliers(SupplierName, Phone, OpeningBalance, IsActive)
    VALUES (N'Default Supplier', NULL, 0, 1);

-- Admin user with LEGACY SHA256 of "admin123" so first login works even before PBKDF2 upgrade
-- SHA256("admin123") uppercase hex:
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserName = N'admin')
    INSERT INTO dbo.Users(UserName, PasswordHash, FullName, Role, IsActive)
    VALUES (
        N'admin',
        N'240BE518FABD2724DDB6F04EEA700D19A5B249B0D3B3F2A8F2E8B2B0B2E0B2E0', -- placeholder; app accepts legacy OR set real hash
        N'System Administrator',
        N'Admin',
        1
    );
GO

-- Fix admin password to known SHA256(admin123) = 240BE518FABD2724DDB6F04EEA700D19A5B249B0...
-- Compute correctly:
-- Actually standard SHA256 of "admin123" is:
-- 240be518fabd2724ddb6f04eea700d19a5b249b0...
UPDATE dbo.Users
SET PasswordHash = UPPER(CONVERT(VARCHAR(64), HASHBYTES('SHA2_256', CONVERT(VARBINARY(100), 'admin123')), 2))
WHERE UserName = N'admin';
GO

-- Invoice counters + defaults
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'NextInvoiceNumber')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'NextInvoiceNumber', N'1');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'InvoicePrefix')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'InvoicePrefix', N'INV-');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'NextPurchaseInvoiceNumber')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'NextPurchaseInvoiceNumber', N'1');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'PurchaseInvoicePrefix')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'PurchaseInvoicePrefix', N'PUR-');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'ShopName')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'ShopName', N'My Shop');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'ShopPhone')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'ShopPhone', N'');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName = N'Theme')
    INSERT INTO dbo.Settings(SettingName, SettingValue) VALUES (N'Theme', N'Blue');
GO

PRINT 'APPLIANCE_DB ready for new customer.';
PRINT 'Login: admin / admin123';
GO
