-- =============================================
-- Author:      Gary Wu
-- Project:     DotNetToolkit Integration Tests
-- Date:        December 1, 2025
-- Description: Initializes a test database with sample tables,
--              relationships, and stored procedures for integration testing.
-- =============================================

USE [DotNetToolkitTest]
GO

-- Drop existing stored procedures
IF OBJECT_ID('dbo.usp_GetUserById', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_GetUserById;
IF OBJECT_ID('dbo.usp_GetAllUsers', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_GetAllUsers;
IF OBJECT_ID('dbo.usp_CreateUser', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_CreateUser;
IF OBJECT_ID('dbo.usp_UpdateUser', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_UpdateUser;
IF OBJECT_ID('dbo.usp_DeleteUser', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_DeleteUser;
IF OBJECT_ID('dbo.usp_GetOrdersByUserId', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_GetOrdersByUserId;
IF OBJECT_ID('dbo.usp_CreateOrder', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_CreateOrder;
IF OBJECT_ID('dbo.usp_GetOrderDetails', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_GetOrderDetails;
IF OBJECT_ID('dbo.usp_AddOrderItem', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_AddOrderItem;
IF OBJECT_ID('dbo.usp_GetProductsByCategory', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_GetProductsByCategory;
IF OBJECT_ID('dbo.usp_UpdateProductPrice', 'P') IS NOT NULL DROP PROCEDURE dbo.usp_UpdateProductPrice;
GO

-- Drop existing tables (in correct order due to FK constraints)
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

-- =============================================
-- Create Tables
-- =============================================

-- Categories table
CREATE TABLE dbo.Categories
(
    CategoryId   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description  NVARCHAR(500) NULL,
    CreatedDate  DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive     BIT NOT NULL DEFAULT 1
);
GO

-- Products table
CREATE TABLE dbo.Products
(
    ProductId   INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(200) NOT NULL,
    CategoryId  INT NOT NULL,
    Price       DECIMAL(18,2) NOT NULL,
    StockQty    INT NOT NULL DEFAULT 0,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive    BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) 
        REFERENCES dbo.Categories(CategoryId),
    CONSTRAINT CHK_Products_Price CHECK (Price >= 0),
    CONSTRAINT CHK_Products_StockQty CHECK (StockQty >= 0)
);
GO

-- Users table
CREATE TABLE dbo.Users
(
    UserId      INT IDENTITY(1,1) PRIMARY KEY,
    Username    NVARCHAR(50) NOT NULL UNIQUE,
    Email       NVARCHAR(100) NOT NULL UNIQUE,
    FirstName   NVARCHAR(50) NOT NULL,
    LastName    NVARCHAR(50) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive    BIT NOT NULL DEFAULT 1
);
GO

-- Orders table
CREATE TABLE dbo.Orders
(
    OrderId     INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT NOT NULL,
    OrderDate   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status      NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) 
        REFERENCES dbo.Users(UserId),
    CONSTRAINT CHK_Orders_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT CHK_Orders_Status CHECK (Status IN ('Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled'))
);
GO

-- OrderItems table
CREATE TABLE dbo.OrderItems
(
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId     INT NOT NULL,
    ProductId   INT NOT NULL,
    Quantity    INT NOT NULL,
    UnitPrice   DECIMAL(18,2) NOT NULL,
    Subtotal    AS (Quantity * UnitPrice) PERSISTED,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) 
        REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) 
        REFERENCES dbo.Products(ProductId),
    CONSTRAINT CHK_OrderItems_Quantity CHECK (Quantity > 0),
    CONSTRAINT CHK_OrderItems_UnitPrice CHECK (UnitPrice >= 0)
);
GO

-- =============================================
-- Create Indexes
-- =============================================
CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);
CREATE NONCLUSTERED INDEX IX_Orders_UserId ON dbo.Orders(UserId);
CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
CREATE NONCLUSTERED INDEX IX_OrderItems_ProductId ON dbo.OrderItems(ProductId);
GO

-- =============================================
-- Seed Initial Data
-- =============================================

-- Seed Categories
INSERT INTO dbo.Categories (CategoryName, Description) VALUES
('Electronics', 'Electronic devices and accessories'),
('Books', 'Physical and digital books'),
('Clothing', 'Apparel and fashion items'),
('Home & Garden', 'Home improvement and gardening supplies');
GO

-- Seed Products
INSERT INTO dbo.Products (ProductName, CategoryId, Price, StockQty) VALUES
('Laptop Pro 15', 1, 1299.99, 50),
('Wireless Mouse', 1, 29.99, 200),
('USB-C Cable', 1, 14.99, 500),
('Programming Guide', 2, 49.99, 100),
('Fiction Novel', 2, 19.99, 150),
('T-Shirt Blue', 3, 24.99, 300),
('Jeans Classic', 3, 59.99, 120),
('Garden Hose', 4, 39.99, 75);
GO

-- Seed Users
INSERT INTO dbo.Users (Username, Email, FirstName, LastName) VALUES
('jdoe', 'john.doe@example.com', 'John', 'Doe'),
('asmith', 'alice.smith@example.com', 'Alice', 'Smith'),
('bwilliams', 'bob.williams@example.com', 'Bob', 'Williams');
GO

-- =============================================
-- Create Stored Procedures
-- =============================================

-- Get User by ID
CREATE PROCEDURE dbo.usp_GetUserById
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT UserId, Username, Email, FirstName, LastName, CreatedDate, IsActive
    FROM dbo.Users
    WHERE UserId = @UserId;
END
GO

-- Get All Users
CREATE PROCEDURE dbo.usp_GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT UserId, Username, Email, FirstName, LastName, CreatedDate, IsActive
    FROM dbo.Users
    WHERE IsActive = 1
    ORDER BY Username;
END
GO

-- Create User
CREATE PROCEDURE dbo.usp_CreateUser
    @Username NVARCHAR(50),
    @Email NVARCHAR(100),
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50),
    @UserId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO dbo.Users (Username, Email, FirstName, LastName)
    VALUES (@Username, @Email, @FirstName, @LastName);
    
    SET @UserId = SCOPE_IDENTITY();
    
    SELECT @UserId AS UserId;
END
GO

-- Update User
CREATE PROCEDURE dbo.usp_UpdateUser
    @UserId INT,
    @Email NVARCHAR(100),
    @FirstName NVARCHAR(50),
    @LastName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Users
    SET Email = @Email,
        FirstName = @FirstName,
        LastName = @LastName
    WHERE UserId = @UserId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Delete User (soft delete)
CREATE PROCEDURE dbo.usp_DeleteUser
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE dbo.Users
    SET IsActive = 0
    WHERE UserId = @UserId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

-- Get Orders by User ID
CREATE PROCEDURE dbo.usp_GetOrdersByUserId
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT o.OrderId, o.UserId, o.OrderDate, o.TotalAmount, o.Status,
           u.Username, u.FirstName, u.LastName
    FROM dbo.Orders o
    INNER JOIN dbo.Users u ON o.UserId = u.UserId
    WHERE o.UserId = @UserId
    ORDER BY o.OrderDate DESC;
END
GO

-- Create Order
CREATE PROCEDURE dbo.usp_CreateOrder
    @UserId INT,
    @Status NVARCHAR(20) = 'Pending',
    @OrderId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate user exists
    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @UserId AND IsActive = 1)
    BEGIN
        RAISERROR('User does not exist or is inactive.', 16, 1);
        RETURN;
    END
    
    INSERT INTO dbo.Orders (UserId, Status)
    VALUES (@UserId, @Status);
    
    SET @OrderId = SCOPE_IDENTITY();
    
    SELECT @OrderId AS OrderId;
END
GO

-- Get Order Details with Items
CREATE PROCEDURE dbo.usp_GetOrderDetails
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Order header
    SELECT o.OrderId, o.UserId, o.OrderDate, o.TotalAmount, o.Status,
           u.Username, u.Email, u.FirstName, u.LastName
    FROM dbo.Orders o
    INNER JOIN dbo.Users u ON o.UserId = u.UserId
    WHERE o.OrderId = @OrderId;
    
    -- Order items
    SELECT oi.OrderItemId, oi.OrderId, oi.ProductId, oi.Quantity, 
           oi.UnitPrice, oi.Subtotal,
           p.ProductName, p.CategoryId
    FROM dbo.OrderItems oi
    INNER JOIN dbo.Products p ON oi.ProductId = p.ProductId
    WHERE oi.OrderId = @OrderId;
END
GO

-- Add Order Item
CREATE PROCEDURE dbo.usp_AddOrderItem
    @OrderId INT,
    @ProductId INT,
    @Quantity INT,
    @OrderItemId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UnitPrice DECIMAL(18,2);
    DECLARE @Subtotal DECIMAL(18,2);
    
    -- Get current product price
    SELECT @UnitPrice = Price
    FROM dbo.Products
    WHERE ProductId = @ProductId AND IsActive = 1;
    
    IF @UnitPrice IS NULL
    BEGIN
        RAISERROR('Product does not exist or is inactive.', 16, 1);
        RETURN;
    END
    
    -- Check stock availability
    IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductId = @ProductId AND StockQty >= @Quantity)
    BEGIN
        RAISERROR('Insufficient stock for this product.', 16, 1);
        RETURN;
    END
    
    SET @Subtotal = @Quantity * @UnitPrice;
    
    BEGIN TRANSACTION;
    
    -- Insert order item
    INSERT INTO dbo.OrderItems (OrderId, ProductId, Quantity, UnitPrice)
    VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice);
    
    SET @OrderItemId = SCOPE_IDENTITY();
    
    -- Update order total
    UPDATE dbo.Orders
    SET TotalAmount = TotalAmount + @Subtotal
    WHERE OrderId = @OrderId;
    
    -- Decrease stock
    UPDATE dbo.Products
    SET StockQty = StockQty - @Quantity
    WHERE ProductId = @ProductId;
    
    COMMIT TRANSACTION;
    
    SELECT @OrderItemId AS OrderItemId;
END
GO

-- Get Products by Category
CREATE PROCEDURE dbo.usp_GetProductsByCategory
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT p.ProductId, p.ProductName, p.CategoryId, p.Price, 
           p.StockQty, p.CreatedDate, p.IsActive,
           c.CategoryName, c.Description
    FROM dbo.Products p
    INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
    WHERE p.CategoryId = @CategoryId AND p.IsActive = 1
    ORDER BY p.ProductName;
END
GO

-- Update Product Price
CREATE PROCEDURE dbo.usp_UpdateProductPrice
    @ProductId INT,
    @NewPrice DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @NewPrice < 0
    BEGIN
        RAISERROR('Price cannot be negative.', 16, 1);
        RETURN;
    END
    
    UPDATE dbo.Products
    SET Price = @NewPrice
    WHERE ProductId = @ProductId;
    
    SELECT @@ROWCOUNT AS RowsAffected;
END
GO

PRINT 'Database initialization completed successfully.';
GO
