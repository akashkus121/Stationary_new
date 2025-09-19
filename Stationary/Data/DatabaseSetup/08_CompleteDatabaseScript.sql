-- =============================================
-- Complete Database Script for Stationary Application
-- Purpose: Create complete database with tables, stored procedures, and sample data
-- Version: 2.0
-- =============================================

PRINT '=============================================================';
PRINT 'Starting Complete Database Setup for Stationary Application';
PRINT '=============================================================';
PRINT '';

-- Create database if it doesn't exist
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'StationaryDB')
BEGIN
    CREATE DATABASE [StationaryDB];
    PRINT 'Database StationaryDB created successfully!';
END
ELSE
BEGIN
    PRINT 'Database StationaryDB already exists.';
END
GO

USE [StationaryDB];
GO

-- =============================================
-- CREATE TABLES
-- =============================================

-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Username] [nvarchar](50) NOT NULL,
        [Password] [nvarchar](100) NOT NULL,
        [Role] [nvarchar](20) NOT NULL,
        [Email] [nvarchar](100) NULL,
        [Phone] [nvarchar](20) NULL,
        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [IsActive] [bit] NOT NULL DEFAULT(1),
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UK_Users_Username] UNIQUE ([Username])
    );
    PRINT 'Users table created successfully!';
END
ELSE
BEGIN
    PRINT 'Users table already exists.';
END

-- Create Products table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Products](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Category] [nvarchar](50) NOT NULL,
        [Price] [decimal](18,2) NOT NULL,
        [ImagePath] [nvarchar](500) NULL,
        [StockQuantity] [int] NOT NULL DEFAULT(0),
        [LowStockThreshold] [int] NOT NULL DEFAULT(10),
        [IsVisible] [bit] NOT NULL DEFAULT(1),
        [Description] [nvarchar](500) NULL,
        [CreatedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] [datetime] NULL,
        [IsActive] [bit] NOT NULL DEFAULT(1),
        CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Products table created successfully!';
END
ELSE
BEGIN
    PRINT 'Products table already exists.';
END

-- Create Carts table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Carts]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Carts](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [ProductId] [int] NOT NULL,
        [Quantity] [int] NOT NULL,
        [AddedDate] [datetime] NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] [datetime] NULL,
        CONSTRAINT [PK_Carts] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Carts table created successfully!';
END
ELSE
BEGIN
    PRINT 'Carts table already exists.';
END

-- Create Orders table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Orders](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [int] NOT NULL,
        [TotalAmount] [decimal](18,2) NOT NULL,
        [Subtotal] [decimal](18,2) NOT NULL,
        [TaxAmount] [decimal](18,2) NOT NULL DEFAULT(0),
        [PaymentMethod] [nvarchar](50) NOT NULL DEFAULT('cash'),
        [OrderStatus] [nvarchar](20) NOT NULL DEFAULT('Pending'),
        [Date] [datetime] NOT NULL DEFAULT GETDATE(),
        [UpdatedDate] [datetime] NULL,
        [Notes] [nvarchar](500) NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'Orders table created successfully!';
END
ELSE
BEGIN
    PRINT 'Orders table already exists.';
END

-- Create OrderItems table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[OrderItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[OrderItems](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [OrderId] [int] NOT NULL,
        [ProductId] [int] NOT NULL,
        [ProductName] [nvarchar](100) NOT NULL,
        [Quantity] [int] NOT NULL,
        [Price] [decimal](18,2) NOT NULL,
        [TotalPrice] [decimal](18,2) NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'OrderItems table created successfully!';
END
ELSE
BEGIN
    PRINT 'OrderItems table already exists.';
END

-- =============================================
-- ADD FOREIGN KEY CONSTRAINTS
-- =============================================

-- Carts foreign keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Carts_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Carts]'))
BEGIN
    ALTER TABLE [dbo].[Carts] ADD CONSTRAINT [FK_Carts_Users] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE;
    PRINT 'Foreign key FK_Carts_Users added successfully!';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Carts_Products]') AND parent_object_id = OBJECT_ID(N'[dbo].[Carts]'))
BEGIN
    ALTER TABLE [dbo].[Carts] ADD CONSTRAINT [FK_Carts_Products] FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE;
    PRINT 'Foreign key FK_Carts_Products added successfully!';
END

-- Orders foreign keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orders_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Orders]'))
BEGIN
    ALTER TABLE [dbo].[Orders] ADD CONSTRAINT [FK_Orders_Users] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([Id]);
    PRINT 'Foreign key FK_Orders_Users added successfully!';
END

-- OrderItems foreign keys
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderItems_Orders]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderItems]'))
BEGIN
    ALTER TABLE [dbo].[OrderItems] ADD CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY([OrderId]) REFERENCES [dbo].[Orders] ([Id]) ON DELETE CASCADE;
    PRINT 'Foreign key FK_OrderItems_Orders added successfully!';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderItems_Products]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderItems]'))
BEGIN
    ALTER TABLE [dbo].[OrderItems] ADD CONSTRAINT [FK_OrderItems_Products] FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products] ([Id]);
    PRINT 'Foreign key FK_OrderItems_Products added successfully!';
END

-- =============================================
-- CREATE INDEXES FOR PERFORMANCE
-- =============================================

-- Users indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'IX_Users_Username')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Users_Username] ON [dbo].[Users] ([Username]);
    PRINT 'Index IX_Users_Username created successfully!';
END

-- Products indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'IX_Products_Category')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Products_Category] ON [dbo].[Products] ([Category]);
    PRINT 'Index IX_Products_Category created successfully!';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'IX_Products_IsVisible')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Products_IsVisible] ON [dbo].[Products] ([IsVisible]);
    PRINT 'Index IX_Products_IsVisible created successfully!';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'IX_Products_StockQuantity')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Products_StockQuantity] ON [dbo].[Products] ([StockQuantity]);
    PRINT 'Index IX_Products_StockQuantity created successfully!';
END

-- Carts indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Carts]') AND name = 'IX_Carts_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Carts_UserId] ON [dbo].[Carts] ([UserId]);
    PRINT 'Index IX_Carts_UserId created successfully!';
END

-- Orders indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = 'IX_Orders_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Orders_UserId] ON [dbo].[Orders] ([UserId]);
    PRINT 'Index IX_Orders_UserId created successfully!';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = 'IX_Orders_Date')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Orders_Date] ON [dbo].[Orders] ([Date]);
    PRINT 'Index IX_Orders_Date created successfully!';
END

-- OrderItems indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[OrderItems]') AND name = 'IX_OrderItems_OrderId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_OrderItems_OrderId] ON [dbo].[OrderItems] ([OrderId]);
    PRINT 'Index IX_OrderItems_OrderId created successfully!';
END

-- =============================================
-- CREATE TABLE TYPE FOR STORED PROCEDURES
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'StockUpdateTableType' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TYPE [dbo].[StockUpdateTableType] AS TABLE
    (
        [ProductId] INT NOT NULL,
        [NewStockQuantity] INT NOT NULL,
        [NewLowStockThreshold] INT NOT NULL,
        [ProductName] NVARCHAR(100) NULL,
        [CurrentStock] INT NULL,
        [CurrentLowStockThreshold] INT NULL
    );
    PRINT 'StockUpdateTableType created successfully!';
END
ELSE
BEGIN
    PRINT 'StockUpdateTableType already exists.';
END

-- =============================================
-- CREATE STORED PROCEDURES
-- =============================================

-- 1. Get Stock Alert Summary
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetStockAlertSummary]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_GetStockAlertSummary]
GO

CREATE PROCEDURE [dbo].[usp_GetStockAlertSummary]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) as TotalProducts,
        SUM(CASE WHEN StockQuantity <= 0 THEN 1 ELSE 0 END) as OutOfStock,
        SUM(CASE WHEN StockQuantity > 0 AND StockQuantity <= LowStockThreshold THEN 1 ELSE 0 END) as LowStock,
        SUM(CASE WHEN StockQuantity > LowStockThreshold THEN 1 ELSE 0 END) as InStock,
        SUM(CASE WHEN IsVisible = 1 THEN 1 ELSE 0 END) as VisibleProducts,
        SUM(CASE WHEN IsVisible = 0 THEN 1 ELSE 0 END) as HiddenProducts
    FROM Products
    WHERE IsActive = 1;
END
GO

-- 2. Get Products by Stock Status
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetProductsByStockStatus]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_GetProductsByStockStatus]
GO

CREATE PROCEDURE [dbo].[usp_GetProductsByStockStatus]
    @StockStatus NVARCHAR(20) = 'all',
    @Category NVARCHAR(50) = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    
    SELECT 
        Id, Name, Category, Price, ImagePath, StockQuantity, 
        LowStockThreshold, IsVisible, Description, CreatedDate
    FROM Products
    WHERE IsActive = 1
    AND (@Category IS NULL OR Category = @Category)
    AND (@SearchTerm IS NULL OR Name LIKE '%' + @SearchTerm + '%')
    AND (
        @StockStatus = 'all' OR
        (@StockStatus = 'instock' AND StockQuantity > LowStockThreshold) OR
        (@StockStatus = 'lowstock' AND StockQuantity > 0 AND StockQuantity <= LowStockThreshold) OR
        (@StockStatus = 'outofstock' AND StockQuantity <= 0)
    )
    ORDER BY Name
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- 3. Bulk Update Stock
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_BulkUpdateStock]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_BulkUpdateStock]
GO

CREATE PROCEDURE [dbo].[usp_BulkUpdateStock]
    @StockUpdates [dbo].[StockUpdateTableType] READONLY
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @UpdatedCount INT = 0;
    DECLARE @ErrorCount INT = 0;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        UPDATE p
        SET 
            StockQuantity = su.NewStockQuantity,
            LowStockThreshold = su.NewLowStockThreshold,
            UpdatedDate = GETDATE()
        FROM Products p
        INNER JOIN @StockUpdates su ON p.Id = su.ProductId
        WHERE p.IsActive = 1;
        
        SET @UpdatedCount = @@ROWCOUNT;
        
        COMMIT TRANSACTION;
        
        SELECT @UpdatedCount as UpdatedProducts, @ErrorCount as Errors;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 4. Get Low Stock Alerts
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetLowStockAlerts]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_GetLowStockAlerts]
GO

CREATE PROCEDURE [dbo].[usp_GetLowStockAlerts]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Id, Name, Category, Price, StockQuantity, LowStockThreshold,
        CASE 
            WHEN StockQuantity <= 0 THEN 'Out of Stock'
            WHEN StockQuantity <= LowStockThreshold THEN 'Low Stock'
            ELSE 'In Stock'
        END as StockStatus
    FROM Products
    WHERE IsActive = 1 
    AND (StockQuantity <= 0 OR StockQuantity <= LowStockThreshold)
    ORDER BY StockQuantity ASC;
END
GO

-- 5. Update Product Visibility
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_UpdateProductVisibility]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_UpdateProductVisibility]
GO

CREATE PROCEDURE [dbo].[usp_UpdateProductVisibility]
    @AutoHideOutOfStock BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @AutoHideOutOfStock = 1
    BEGIN
        UPDATE Products 
        SET IsVisible = 0, UpdatedDate = GETDATE()
        WHERE StockQuantity <= 0 AND IsActive = 1;
        
        SELECT @@ROWCOUNT AS HiddenProducts;
    END
    ELSE
    BEGIN
        UPDATE Products 
        SET IsVisible = 1, UpdatedDate = GETDATE()
        WHERE IsActive = 1;
        
        SELECT @@ROWCOUNT AS ShownProducts;
    END
END
GO

-- 6. Get Cart Items with Product Details
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetCartItems]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_GetCartItems]
GO

CREATE PROCEDURE [dbo].[usp_GetCartItems]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        c.Id as CartId,
        c.Quantity,
        c.AddedDate,
        p.Id as ProductId,
        p.Name,
        p.Category,
        p.Price,
        p.ImagePath,
        p.StockQuantity,
        p.LowStockThreshold,
        p.IsVisible,
        (c.Quantity * p.Price) as ItemTotal
    FROM Carts c
    INNER JOIN Products p ON c.ProductId = p.Id
    WHERE c.UserId = @UserId AND p.IsActive = 1
    ORDER BY c.AddedDate DESC;
END
GO

-- 7. Create Order with Items
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_CreateOrder]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_CreateOrder]
GO

CREATE PROCEDURE [dbo].[usp_CreateOrder]
    @UserId INT,
    @Subtotal DECIMAL(18,2),
    @TaxAmount DECIMAL(18,2),
    @TotalAmount DECIMAL(18,2),
    @PaymentMethod NVARCHAR(50) = 'cash',
    @OrderStatus NVARCHAR(20) = 'Pending',
    @Notes NVARCHAR(500) = NULL,
    @OrderId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Create order
        INSERT INTO Orders (UserId, Subtotal, TaxAmount, TotalAmount, PaymentMethod, OrderStatus, Notes)
        VALUES (@UserId, @Subtotal, @TaxAmount, @TotalAmount, @PaymentMethod, @OrderStatus, @Notes);
        
        SET @OrderId = SCOPE_IDENTITY();
        
        -- Move cart items to order items
        INSERT INTO OrderItems (OrderId, ProductId, ProductName, Quantity, Price, TotalPrice)
        SELECT 
            @OrderId,
            p.Id,
            p.Name,
            c.Quantity,
            p.Price,
            (c.Quantity * p.Price)
        FROM Carts c
        INNER JOIN Products p ON c.ProductId = p.Id
        WHERE c.UserId = @UserId;
        
        -- Update product stock
        UPDATE p
        SET 
            StockQuantity = p.StockQuantity - c.Quantity,
            UpdatedDate = GETDATE()
        FROM Products p
        INNER JOIN Carts c ON p.Id = c.ProductId
        WHERE c.UserId = @UserId;
        
        -- Clear user's cart
        DELETE FROM Carts WHERE UserId = @UserId;
        
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 8. Get Order History
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetOrderHistory]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_GetOrderHistory]
GO

CREATE PROCEDURE [dbo].[usp_GetOrderHistory]
    @UserId INT,
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    
    SELECT 
        o.Id,
        o.TotalAmount,
        o.Subtotal,
        o.TaxAmount,
        o.PaymentMethod,
        o.OrderStatus,
        o.Date,
        o.Notes,
        COUNT(oi.Id) as ItemCount
    FROM Orders o
    LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
    WHERE o.UserId = @UserId
    GROUP BY o.Id, o.TotalAmount, o.Subtotal, o.TaxAmount, o.PaymentMethod, o.OrderStatus, o.Date, o.Notes
    ORDER BY o.Date DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- 9. Get Order Details
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetOrderDetails]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_GetOrderDetails]
GO

CREATE PROCEDURE [dbo].[usp_GetOrderDetails]
    @OrderId INT,
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Get order info
    SELECT 
        o.Id,
        o.TotalAmount,
        o.Subtotal,
        o.TaxAmount,
        o.PaymentMethod,
        o.OrderStatus,
        o.Date,
        o.Notes,
        u.Username
    FROM Orders o
    INNER JOIN Users u ON o.UserId = u.Id
    WHERE o.Id = @OrderId AND o.UserId = @UserId;
    
    -- Get order items
    SELECT 
        oi.Id,
        oi.ProductId,
        oi.ProductName,
        oi.Quantity,
        oi.Price,
        oi.TotalPrice
    FROM OrderItems oi
    WHERE oi.OrderId = @OrderId
    ORDER BY oi.ProductName;
END
GO

-- 10. Get Sales Report
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetSalesReport]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_GetSalesReport]
GO

CREATE PROCEDURE [dbo].[usp_GetSalesReport]
    @StartDate DATE = NULL,
    @EndDate DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    IF @StartDate IS NULL SET @StartDate = CAST(GETDATE() AS DATE);
    IF @EndDate IS NULL SET @EndDate = CAST(GETDATE() AS DATE);
    
    SELECT 
        COUNT(*) as TotalOrders,
        SUM(TotalAmount) as TotalRevenue,
        SUM(Subtotal) as TotalSubtotal,
        SUM(TaxAmount) as TotalTax,
        AVG(TotalAmount) as AverageOrderValue,
        COUNT(DISTINCT UserId) as UniqueCustomers
    FROM Orders
    WHERE CAST(Date AS DATE) BETWEEN @StartDate AND @EndDate;
    
    -- Top selling products
    SELECT TOP 10
        oi.ProductName,
        SUM(oi.Quantity) as TotalQuantity,
        SUM(oi.TotalPrice) as TotalRevenue,
        COUNT(DISTINCT oi.OrderId) as OrderCount
    FROM OrderItems oi
    INNER JOIN Orders o ON oi.OrderId = o.Id
    WHERE CAST(o.Date AS DATE) BETWEEN @StartDate AND @EndDate
    GROUP BY oi.ProductName
    ORDER BY TotalQuantity DESC;
END
GO

-- =============================================
-- INSERT SAMPLE DATA
-- =============================================

-- Insert sample users
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Password, Role, Email, Phone) VALUES 
    ('admin', 'admin123', 'Admin', 'admin@stationary.com', '1234567890'),
    ('user', 'user123', 'User', 'user@stationary.com', '0987654321'),
    ('akash', 'akash123', 'Admin', 'akash@stationary.com', '9087654321');
    PRINT 'Sample users created successfully!';
END
ELSE
BEGIN
    PRINT 'Sample users already exist.';
END

-- Insert sample products
IF NOT EXISTS (SELECT * FROM Products)
BEGIN
    INSERT INTO Products (Name, Category, Price, StockQuantity, LowStockThreshold, IsVisible, Description) VALUES 
    ('Blue Ballpoint Pen', 'Pens', 1.99, 50, 10, 1, 'Smooth writing blue ballpoint pen'),
    ('Red Ballpoint Pen', 'Pens', 1.99, 45, 10, 1, 'Smooth writing red ballpoint pen'),
    ('A4 Notebook 200 Pages', 'Notebooks', 5.99, 30, 5, 1, 'High quality A4 notebook with 200 pages'),
    ('A5 Notebook 100 Pages', 'Notebooks', 3.99, 25, 5, 1, 'Compact A5 notebook with 100 pages'),
    ('Heavy Duty Stapler', 'Office Supplies', 12.99, 15, 3, 1, 'Professional heavy duty stapler'),
    ('Paper Clips Box (100)', 'Office Supplies', 2.99, 100, 20, 1, 'Box of 100 assorted paper clips'),
    ('Yellow Highlighter', 'Markers', 1.49, 25, 8, 1, 'Bright yellow highlighter marker'),
    ('Pink Highlighter', 'Markers', 1.49, 20, 8, 1, 'Bright pink highlighter marker'),
    ('Whiteboard Marker Set', 'Markers', 4.99, 12, 5, 1, 'Set of 4 whiteboard markers'),
    ('Eraser', 'Office Supplies', 0.99, 50, 15, 1, 'Soft rubber eraser'),
    ('Ruler 30cm', 'Office Supplies', 1.99, 30, 10, 1, 'Transparent 30cm ruler'),
    ('Calculator', 'Electronics', 15.99, 8, 3, 1, 'Basic scientific calculator');
    PRINT 'Sample products created successfully!';
END
ELSE
BEGIN
    PRINT 'Sample products already exist.';
END

PRINT '';
PRINT '=============================================================';
PRINT 'Complete Database Setup Finished Successfully!';
PRINT '=============================================================';
PRINT '';
PRINT 'Database: StationaryDB';
PRINT 'Tables: Users, Products, Carts, Orders, OrderItems';
PRINT 'Stored Procedures: 10 comprehensive procedures created';
PRINT 'Indexes: Performance indexes created for optimal performance';
PRINT '';
PRINT 'Default credentials:';
PRINT 'Admin: admin / admin123';
PRINT 'User: user / user123';
PRINT 'Akash: akash / akash123';
PRINT '';
PRINT 'Features included:';
PRINT '- Complete cart management with UPI payment support';
PRINT '- Stock management with low stock alerts';
PRINT '- Order management with detailed reporting';
PRINT '- User management with roles';
PRINT '- Product visibility controls';
PRINT '- Sales reporting and analytics';
PRINT '';
PRINT 'Your Stationary application database is now ready!';
