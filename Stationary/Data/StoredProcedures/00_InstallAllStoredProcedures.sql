-- =============================================
-- Master Script: Install All Stock Management Stored Procedures
-- Purpose: Install all stored procedures in the correct order
-- =============================================

PRINT 'Starting installation of Stock Management Stored Procedures...';
PRINT '=============================================================';

-- First, create the table type if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.types WHERE name = 'StockUpdateTableType' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    PRINT 'Creating StockUpdateTableType...';
    
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

PRINT '';

-- Now install all stored procedures
PRINT 'Installing stored procedures...';
PRINT '--------------------------------';

-- Note: The individual SP files should be executed in order:
-- 1. 01_StockAlertSummary.sql
-- 2. 02_ProductsByStockStatus.sql  
-- 3. 03_BulkUpdateStock.sql
-- 4. 04_LowStockAlerts.sql
-- 5. 05_UpdateProductVisibility.sql

PRINT 'Please execute the following files in order:';
PRINT '1. 01_StockAlertSummary.sql';
PRINT '2. 02_ProductsByStockStatus.sql';
PRINT '3. 03_BulkUpdateStock.sql';
PRINT '4. 04_LowStockAlerts.sql';
PRINT '5. 05_UpdateProductVisibility.sql';

PRINT '';
PRINT 'Installation script completed!';
PRINT '=============================================================';