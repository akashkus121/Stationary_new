-- =============================================
-- Performance Indexes for Stock Management
-- Purpose: Create indexes for better query performance
-- =============================================

USE [StationaryDB];
GO

PRINT 'Creating performance indexes...';

-- Index for stock quantity filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_StockQuantity' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Products_StockQuantity] ON [dbo].[Products]
    (
        [StockQuantity] ASC
    )
    INCLUDE ([Name], [Category], [Price], [ImagePath], [LowStockThreshold], [IsVisible]);
    PRINT 'Index IX_Products_StockQuantity created successfully!';
END
ELSE
BEGIN
    PRINT 'Index IX_Products_StockQuantity already exists.';
END

-- Index for low stock threshold queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_LowStockThreshold' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Products_LowStockThreshold] ON [dbo].[Products]
    (
        [LowStockThreshold] ASC,
        [StockQuantity] ASC
    )
    INCLUDE ([Name], [Category], [Price], [ImagePath], [IsVisible]);
    PRINT 'Index IX_Products_LowStockThreshold created successfully!';
END
ELSE
BEGIN
    PRINT 'Index IX_Products_LowStockThreshold already exists.';
END

-- Index for product visibility and stock status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Visibility_Stock' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Products_Visibility_Stock] ON [dbo].[Products]
    (
        [IsVisible] ASC,
        [StockQuantity] ASC
    )
    INCLUDE ([Name], [Category], [Price], [ImagePath], [LowStockThreshold]);
    PRINT 'Index IX_Products_Visibility_Stock created successfully!';
END
ELSE
BEGIN
    PRINT 'Index IX_Products_Visibility_Stock already exists.';
END

-- Index for category filtering with stock
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Category_Stock' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Products_Category_Stock] ON [dbo].[Products]
    (
        [Category] ASC,
        [StockQuantity] ASC
    )
    INCLUDE ([Name], [Price], [ImagePath], [LowStockThreshold], [IsVisible]);
    PRINT 'Index IX_Products_Category_Stock created successfully!';
END
ELSE
BEGIN
    PRINT 'Index IX_Products_Category_Stock already exists.';
END

-- Index for search with stock status
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Name_Stock' AND object_id = OBJECT_ID('Products'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Products_Name_Stock] ON [dbo].[Products]
    (
        [Name] ASC
    )
    INCLUDE ([Category], [Price], [ImagePath], [StockQuantity], [LowStockThreshold], [IsVisible]);
    PRINT 'Index IX_Products_Name_Stock created successfully!';
END
ELSE
BEGIN
    PRINT 'Index IX_Products_Name_Stock already exists.';
END

-- Index for Users table
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Users_Username] ON [dbo].[Users]
    (
        [Username] ASC
    );
    PRINT 'Index IX_Users_Username created successfully!';
END
ELSE
BEGIN
    PRINT 'Index IX_Users_Username already exists.';
END

-- Index for Carts table
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Carts_UserId_ProductId' AND object_id = OBJECT_ID('Carts'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Carts_UserId_ProductId] ON [dbo].[Carts]
    (
        [UserId] ASC,
        [ProductId] ASC
    );
    PRINT 'Index IX_Carts_UserId_ProductId created successfully!';
END
ELSE
BEGIN
    PRINT 'Index IX_Carts_UserId_ProductId already exists.';
END

PRINT 'All performance indexes created successfully!';
GO