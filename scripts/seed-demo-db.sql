-- ============================================================
-- Sentinel Demo Database Seed Script
-- Run against LocalDB or any SQL Server instance:
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -i seed-demo-db.sql
-- ============================================================

-- Create database if it doesn't exist
IF DB_ID('SentinelDemo') IS NULL
    CREATE DATABASE SentinelDemo;
GO

USE SentinelDemo;
GO

-- ── Customers ──
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
GO

CREATE TABLE dbo.Customers (
    CustomerId   INT PRIMARY KEY IDENTITY(1,1),
    FirstName    NVARCHAR(50)  NOT NULL,
    LastName     NVARCHAR(50)  NOT NULL,
    Email        NVARCHAR(100) NOT NULL,
    City         NVARCHAR(50)  NOT NULL,
    State        NVARCHAR(2)   NOT NULL
);

CREATE TABLE dbo.Products (
    ProductId     INT PRIMARY KEY IDENTITY(1,1),
    Name          NVARCHAR(100) NOT NULL,
    Category      NVARCHAR(50)  NOT NULL,
    Price         DECIMAL(10,2) NOT NULL,
    StockQuantity INT           NOT NULL
);

CREATE TABLE dbo.Orders (
    OrderId      INT PRIMARY KEY IDENTITY(1000,1),
    CustomerId   INT           NOT NULL REFERENCES dbo.Customers(CustomerId),
    OrderDate    DATE          NOT NULL DEFAULT GETDATE(),
    TotalAmount  DECIMAL(10,2) NOT NULL,
    Status       NVARCHAR(20)  NOT NULL DEFAULT 'Processing'
);

CREATE TABLE dbo.OrderItems (
    OrderItemId  INT PRIMARY KEY IDENTITY(1,1),
    OrderId      INT           NOT NULL REFERENCES dbo.Orders(OrderId),
    ProductId    INT           NOT NULL REFERENCES dbo.Products(ProductId),
    Quantity     INT           NOT NULL,
    UnitPrice    DECIMAL(10,2) NOT NULL
);
GO

-- ── Seed Customers ──
INSERT INTO dbo.Customers (FirstName, LastName, Email, City, State) VALUES
('Alice',   'Chen',       'alice@example.com',   'Seattle',   'WA'),
('Bob',     'Martinez',   'bob@example.com',     'Austin',    'TX'),
('Carol',   'Johnson',    'carol@example.com',   'Denver',    'CO'),
('David',   'Kim',        'david@example.com',   'Portland',  'OR'),
('Eva',     'Patel',      'eva@example.com',     'Chicago',   'IL'),
('Frank',   'O''Brien',   'frank@example.com',   'Boston',    'MA'),
('Grace',   'Williams',   'grace@example.com',   'Miami',     'FL'),
('Henry',   'Tanaka',     'henry@example.com',   'San Jose',  'CA');

-- ── Seed Products ──
INSERT INTO dbo.Products (Name, Category, Price, StockQuantity) VALUES
('Wireless Keyboard',    'Electronics', 49.99,  150),
('USB-C Hub',            'Electronics', 34.99,  200),
('Standing Desk Mat',    'Office',      89.00,   75),
('Monitor Light Bar',    'Electronics', 59.99,  120),
('Ergonomic Mouse',      'Electronics', 39.99,  180),
('Webcam HD 1080p',      'Electronics', 79.99,   90),
('Desk Organizer',       'Office',      24.99,  250),
('Noise-Cancel Headset', 'Electronics', 149.99,  60);

-- ── Seed Orders ──
INSERT INTO dbo.Orders (CustomerId, OrderDate, TotalAmount, Status) VALUES
(1, '2026-02-10', 134.98, 'Delivered'),
(2, '2026-02-15',  89.00, 'Delivered'),
(3, '2026-03-01', 299.97, 'Shipped'),
(1, '2026-03-05',  79.99, 'Shipped'),
(4, '2026-03-10', 1249.90, 'Shipped'),
(5, '2026-03-12',  49.99, 'Processing'),
(3, '2026-03-18', 149.50, 'Processing'),
(6, '2026-03-20',  34.99, 'Processing'),
(7, '2026-03-22', 174.98, 'Processing'),
(8, '2026-03-25', 239.97, 'Processing');

-- ── Seed OrderItems ──
INSERT INTO dbo.OrderItems (OrderId, ProductId, Quantity, UnitPrice) VALUES
(1000, 1, 1, 49.99),
(1000, 5, 1, 39.99),
(1000, 2, 1, 34.99),
(1001, 3, 1, 89.00),
(1002, 6, 1, 79.99),
(1002, 4, 1, 59.99),
(1002, 8, 1, 149.99),
(1003, 6, 1, 79.99),
(1004, 8, 5, 149.99),
(1004, 1, 5, 49.99),
(1005, 1, 1, 49.99),
(1006, 8, 1, 149.99),
(1007, 2, 1, 34.99),
(1008, 5, 2, 39.99),
(1008, 1, 1, 49.99),
(1009, 4, 2, 59.99),
(1009, 3, 1, 89.00);
GO

-- ── Verify ──
SELECT 'Customers' AS [Table], COUNT(*) AS [Rows] FROM dbo.Customers
UNION ALL
SELECT 'Products',  COUNT(*) FROM dbo.Products
UNION ALL
SELECT 'Orders',    COUNT(*) FROM dbo.Orders
UNION ALL
SELECT 'OrderItems', COUNT(*) FROM dbo.OrderItems;
GO

PRINT '✓ SentinelDemo database seeded successfully.';
GO
