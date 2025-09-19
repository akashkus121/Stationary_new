-- =============================================
-- Master Installation Script for Stationary Application
-- Purpose: Install database, tables, indexes, and stored procedures
-- =============================================

PRINT '=============================================================';
PRINT 'Starting Master Installation for Stationary Application';
PRINT '=============================================================';
PRINT '';

-- Step 1: Create database and tables
PRINT 'Step 1: Creating database and tables...';
PRINT '----------------------------------------';
:r "01_CreateDatabase.sql"
PRINT '';

-- Step 2: Create performance indexes
PRINT 'Step 2: Creating performance indexes...';
PRINT '----------------------------------------';
:r "02_CreateIndexes.sql"
PRINT '';

-- Step 3: Create table type for stored procedures
PRINT 'Step 3: Creating table type...';
PRINT '----------------------------------------';
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
PRINT '';

-- Step 4: Install stored procedures
PRINT 'Step 4: Installing stored procedures...';
PRINT '----------------------------------------';
:r "../StoredProcedures/01_StockAlertSummary.sql"
:r "../StoredProcedures/02_ProductsByStockStatus.sql"
:r "../StoredProcedures/03_BulkUpdateStock.sql"
:r "../StoredProcedures/04_LowStockAlerts.sql"
:r "../StoredProcedures/05_UpdateProductVisibility.sql"
PRINT '';

-- Step 4.5: Add Product stock fields (if needed)
PRINT 'Step 4.5: Adding Product stock fields...';
PRINT '----------------------------------------';
:r "06_AddProductStockFields.sql"
PRINT '';

-- Step 5: Insert sample data (optional)
PRINT 'Step 5: Inserting sample data...';
PRINT '----------------------------------------';
IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (Username, Password, Role) VALUES ('admin', 'admin123', 'Admin');
    INSERT INTO Users (Username, Password, Role) VALUES ('user', 'user123', 'User');
    PRINT 'Sample users created successfully!';
END
ELSE
BEGIN
    PRINT 'Sample users already exist.';
END

IF NOT EXISTS (SELECT * FROM Products)
BEGIN
    INSERT INTO Products (Name, Category, Price, StockQuantity, LowStockThreshold, IsVisible) VALUES 
    ('Blue Pen', 'Pens', 1.99, 50, 10, 1),
    ('Notebook A4', 'Notebooks', 5.99, 30, 5, 1),
    ('Stapler', 'Office Supplies', 8.99, 15, 3, 1),
    ('Paper Clips', 'Office Supplies', 2.99, 100, 20, 1),
    ('Highlighter', 'Markers', 1.49, 25, 8, 1);
    PRINT 'Sample products created successfully!';
END
ELSE
BEGIN
    PRINT 'Sample products already exist.';
END
PRINT '';

PRINT '=============================================================';
PRINT 'Master Installation Completed Successfully!';
PRINT '=============================================================';
PRINT '';
PRINT 'Your Stationary application database is now ready!';
PRINT 'You can now run your application.';
PRINT '';
PRINT 'Default credentials:';
PRINT 'Admin: admin / admin123';
PRINT 'User: user / user123';
PRINT '';
PRINT 'Database: StationaryDB';
PRINT 'Tables: Users, Products, Carts, Orders, OrderItems';
PRINT 'Stored Procedures: 5 stock management procedures installed';
PRINT 'Indexes: Performance indexes created for optimal performance';