/*
  ApplianceX V6 — SINGLE MASTER DATABASE SCRIPT
  Always update THIS file when schema changes.
  SQL Server 2008+ / Express compatible.
*/
IF DB_ID(N'APPLIANCE_DB') IS NULL
    CREATE DATABASE APPLIANCE_DB;
GO
USE APPLIANCE_DB;
GO
SET NOCOUNT ON;

-- Users
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    UserName NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    FullName NVARCHAR(100) NULL,
    Role NVARCHAR(20) NOT NULL DEFAULT N'Cashier',
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- Categories
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- Products (PurchasePrice/SalePrice = PACK prices)
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductCode NVARCHAR(30) NOT NULL UNIQUE,
    Barcode NVARCHAR(50) NULL,
    ProductName NVARCHAR(200) NOT NULL,
    CategoryID INT NULL REFERENCES dbo.Categories(CategoryID),
    PurchasePrice DECIMAL(18,4) NOT NULL DEFAULT 0,
    SalePrice DECIMAL(18,4) NOT NULL DEFAULT 0,
    MinimumStock INT NOT NULL DEFAULT 0,
    CurrentStock INT NOT NULL DEFAULT 0,
    UnitOfMeasure NVARCHAR(30) NULL,
    PackSize DECIMAL(18,4) NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO
IF COL_LENGTH('dbo.Products','PackSize') IS NULL
    ALTER TABLE dbo.Products ADD PackSize DECIMAL(18,4) NOT NULL DEFAULT 1;
IF COL_LENGTH('dbo.Products','UnitOfMeasure') IS NULL
    ALTER TABLE dbo.Products ADD UnitOfMeasure NVARCHAR(30) NULL;
IF COL_LENGTH('dbo.Products','CurrentStock') IS NULL
    ALTER TABLE dbo.Products ADD CurrentStock INT NOT NULL DEFAULT 0;
GO

-- Customers / Suppliers
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
CREATE TABLE dbo.Customers (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(30) NULL,
    Address NVARCHAR(250) NULL,
    OpeningBalance DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO
IF OBJECT_ID(N'dbo.Suppliers', N'U') IS NULL
CREATE TABLE dbo.Suppliers (
    SupplierID INT IDENTITY(1,1) PRIMARY KEY,
    SupplierName NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(30) NULL,
    Address NVARCHAR(250) NULL,
    OpeningBalance DECIMAL(18,2) NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- Sale
IF OBJECT_ID(N'dbo.SaleHeader', N'U') IS NULL
CREATE TABLE dbo.SaleHeader (
    SaleID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNo NVARCHAR(30) NOT NULL UNIQUE,
    SaleDate DATETIME NOT NULL,
    CustomerID INT NULL REFERENCES dbo.Customers(CustomerID),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    NetAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    BalanceAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Remarks NVARCHAR(250) NULL
);
GO
IF OBJECT_ID(N'dbo.SaleDetail', N'U') IS NULL
CREATE TABLE dbo.SaleDetail (
    SaleDetailID INT IDENTITY(1,1) PRIMARY KEY,
    SaleID INT NOT NULL REFERENCES dbo.SaleHeader(SaleID),
    ProductID INT NOT NULL REFERENCES dbo.Products(ProductID),
    Quantity INT NOT NULL,
    SalePrice DECIMAL(18,4) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Amount DECIMAL(18,2) NOT NULL
);
GO

-- Purchase
IF OBJECT_ID(N'dbo.PurchaseHeader', N'U') IS NULL
CREATE TABLE dbo.PurchaseHeader (
    PurchaseID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceNo NVARCHAR(30) NOT NULL UNIQUE,
    PurchaseDate DATETIME NOT NULL,
    SupplierID INT NOT NULL REFERENCES dbo.Suppliers(SupplierID),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    NetAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    BalanceAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Remarks NVARCHAR(250) NULL
);
GO
IF OBJECT_ID(N'dbo.PurchaseDetail', N'U') IS NULL
CREATE TABLE dbo.PurchaseDetail (
    PurchaseDetailID INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseID INT NOT NULL REFERENCES dbo.PurchaseHeader(PurchaseID),
    ProductID INT NOT NULL REFERENCES dbo.Products(ProductID),
    Quantity INT NOT NULL,
    PurchasePrice DECIMAL(18,4) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Amount DECIMAL(18,2) NOT NULL
);
GO

-- Inventory ledger (source of truth)
IF OBJECT_ID(N'dbo.InventoryTransaction', N'U') IS NULL
CREATE TABLE dbo.InventoryTransaction (
    TransactionID INT IDENTITY(1,1) PRIMARY KEY,
    TransactionDate DATETIME NOT NULL DEFAULT GETDATE(),
    ProductID INT NOT NULL REFERENCES dbo.Products(ProductID),
    TransactionType NVARCHAR(30) NOT NULL,
    ReferenceID INT NULL,
    QuantityIn INT NOT NULL DEFAULT 0,
    QuantityOut INT NOT NULL DEFAULT 0,
    UnitCost DECIMAL(18,4) NOT NULL DEFAULT 0,
    Remarks NVARCHAR(250) NULL
);
GO

-- Sale return
IF OBJECT_ID(N'dbo.SaleReturnHeader', N'U') IS NULL
CREATE TABLE dbo.SaleReturnHeader (
    ReturnID INT IDENTITY(1,1) PRIMARY KEY,
    ReturnNo NVARCHAR(30) NOT NULL UNIQUE,
    ReturnDate DATETIME NOT NULL,
    CustomerID INT NULL,
    OriginalSaleID INT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    NetAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Remarks NVARCHAR(250) NULL
);
GO
IF OBJECT_ID(N'dbo.SaleReturnDetail', N'U') IS NULL
CREATE TABLE dbo.SaleReturnDetail (
    ReturnDetailID INT IDENTITY(1,1) PRIMARY KEY,
    ReturnID INT NOT NULL REFERENCES dbo.SaleReturnHeader(ReturnID),
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    SalePrice DECIMAL(18,4) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Amount DECIMAL(18,2) NOT NULL
);
GO

-- Ledgers
IF OBJECT_ID(N'dbo.CustomerLedger', N'U') IS NULL
CREATE TABLE dbo.CustomerLedger (
    EntryID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    EntryDate DATETIME NOT NULL DEFAULT GETDATE(),
    EntryType NVARCHAR(30) NOT NULL,
    ReferenceID INT NULL,
    ReferenceNo NVARCHAR(40) NULL,
    Debit DECIMAL(18,2) NOT NULL DEFAULT 0,
    Credit DECIMAL(18,2) NOT NULL DEFAULT 0,
    Remarks NVARCHAR(250) NULL,
    CreatedBy INT NULL
);
GO
IF OBJECT_ID(N'dbo.SupplierLedger', N'U') IS NULL
CREATE TABLE dbo.SupplierLedger (
    EntryID INT IDENTITY(1,1) PRIMARY KEY,
    SupplierID INT NOT NULL,
    EntryDate DATETIME NOT NULL DEFAULT GETDATE(),
    EntryType NVARCHAR(30) NOT NULL,
    ReferenceID INT NULL,
    ReferenceNo NVARCHAR(40) NULL,
    Debit DECIMAL(18,2) NOT NULL DEFAULT 0,
    Credit DECIMAL(18,2) NOT NULL DEFAULT 0,
    Remarks NVARCHAR(250) NULL,
    CreatedBy INT NULL
);
GO

-- Settings / counters
IF OBJECT_ID(N'dbo.Settings', N'U') IS NULL
CREATE TABLE dbo.Settings (
    SettingName NVARCHAR(100) NOT NULL PRIMARY KEY,
    SettingValue NVARCHAR(500) NULL
);
GO

-- Seed
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
    INSERT INTO dbo.Categories(CategoryName) VALUES (N'General');
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerName=N'Walk-in Customer')
    INSERT INTO dbo.Customers(CustomerName,OpeningBalance) VALUES (N'Walk-in Customer',0);
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserName=N'admin')
    INSERT INTO dbo.Users(UserName,PasswordHash,FullName,Role)
    VALUES (N'admin', N'8C6976E5B5410415BDE908BD4DEE15DFB167A9C873FC4BB8A81F6F2AB448A918', N'Administrator', N'Admin');
    -- legacy SHA256 of "admin" — app upgrades to PBKDF2 on first login
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName=N'NextSaleInvoiceNumber')
    INSERT INTO dbo.Settings(SettingName,SettingValue) VALUES (N'NextSaleInvoiceNumber',N'1');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName=N'NextPurchaseInvoiceNumber')
    INSERT INTO dbo.Settings(SettingName,SettingValue) VALUES (N'NextPurchaseInvoiceNumber',N'1');
IF NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE SettingName=N'NextProductCode')
    INSERT INTO dbo.Settings(SettingName,SettingValue) VALUES (N'NextProductCode',N'1');
GO
PRINT 'APPLIANCE_DB ready.';
GO
