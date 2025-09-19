-- =============================================
-- Script: Add Product Stock Fields
-- Description: Adds missing stock-related columns to Products table
-- Date: 2024
-- =============================================

USE [ECommerceDB]
GO

PRINT 'Starting to add Product stock fields...'

-- Check if StockQuantity column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'StockQuantity')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [StockQuantity] INT NOT NULL DEFAULT 0
    PRINT 'StockQuantity column added successfully!'
END
ELSE
BEGIN
    PRINT 'StockQuantity column already exists.'
END

-- Check if LowStockThreshold column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'LowStockThreshold')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [LowStockThreshold] INT NOT NULL DEFAULT 5
    PRINT 'LowStockThreshold column added successfully!'
END
ELSE
BEGIN
    PRINT 'LowStockThreshold column already exists.'
END

-- Check if IsVisible column exists, if not add it
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'IsVisible')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [IsVisible] BIT NOT NULL DEFAULT 1
    PRINT 'IsVisible column added successfully!'
END
ELSE
BEGIN
    PRINT 'IsVisible column already exists.'
END

-- Update existing products to have reasonable default values
UPDATE [dbo].[Products] 
SET [StockQuantity] = 10, 
    [LowStockThreshold] = 5, 
    [IsVisible] = 1
WHERE [StockQuantity] IS NULL 
   OR [LowStockThreshold] IS NULL 
   OR [IsVisible] IS NULL

PRINT 'Updated existing products with default stock values.'

-- Add indexes for better performance on stock-related queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'IX_Products_StockQuantity')
BEGIN
    CREATE INDEX [IX_Products_StockQuantity] ON [dbo].[Products] ([StockQuantity])
    PRINT 'Index on StockQuantity created successfully!'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'IX_Products_IsVisible')
BEGIN
    CREATE INDEX [IX_Products_IsVisible] ON [dbo].[Products] ([IsVisible])
    PRINT 'Index on IsVisible created successfully!'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = 'IX_Products_Category_IsVisible')
BEGIN
    CREATE INDEX [IX_Products_Category_IsVisible] ON [dbo].[Products] ([Category], [IsVisible])
    PRINT 'Composite index on Category and IsVisible created successfully!'
END

PRINT 'Product stock fields setup completed successfully!'
GO

