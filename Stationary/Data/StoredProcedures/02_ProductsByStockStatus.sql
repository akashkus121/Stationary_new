-- =============================================
-- Stored Procedure: usp_GetProductsByStockStatus
-- Purpose: Get products filtered by stock status with pagination and search
-- =============================================

CREATE PROCEDURE [dbo].[usp_GetProductsByStockStatus]
    @StockStatus NVARCHAR(20), -- 'available', 'lowstock', 'outofstock', 'all'
    @Category NVARCHAR(50) = NULL,
    @SearchTerm NVARCHAR(100) = NULL,
    @Page INT = 1,
    @PageSize INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    
    -- Build dynamic SQL for flexible filtering
    DECLARE @SQL NVARCHAR(MAX) = '
    SELECT 
        Id, Name, Category, Price, ImagePath, StockQuantity, LowStockThreshold, IsVisible,
        CASE 
            WHEN StockQuantity <= 0 THEN ''Out of Stock''
            WHEN StockQuantity <= LowStockThreshold THEN ''Low Stock ('' + CAST(StockQuantity AS NVARCHAR) + '' left)''
            ELSE ''In Stock ('' + CAST(StockQuantity AS NVARCHAR) + '' available)''
        END AS StockStatus
    FROM Products
    WHERE IsVisible = 1';
    
    -- Add stock status filter
    IF @StockStatus = 'available'
        SET @SQL = @SQL + ' AND StockQuantity > LowStockThreshold';
    ELSE IF @StockStatus = 'lowstock'
        SET @SQL = @SQL + ' AND StockQuantity > 0 AND StockQuantity <= LowStockThreshold';
    ELSE IF @StockStatus = 'outofstock'
        SET @SQL = @SQL + ' AND StockQuantity <= 0';
    -- 'all' shows everything
    
    -- Add category filter
    IF @Category IS NOT NULL
        SET @SQL = @SQL + ' AND Category = @Category';
    
    -- Add search filter
    IF @SearchTerm IS NOT NULL
        SET @SQL = @SQL + ' AND (Name LIKE ''%'' + @SearchTerm + ''%'' OR Category LIKE ''%'' + @SearchTerm + ''%'')';
    
    -- Add pagination
    SET @SQL = @SQL + ' ORDER BY Name OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY';
    
    EXEC sp_executesql @SQL, 
        N'@Category NVARCHAR(50), @SearchTerm NVARCHAR(100), @Offset INT, @PageSize INT',
        @Category, @SearchTerm, @Offset, @PageSize;
END
GO

PRINT 'Stored Procedure usp_GetProductsByStockStatus created successfully!';