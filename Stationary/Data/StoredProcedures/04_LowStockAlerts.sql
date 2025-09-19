-- =============================================
-- Stored Procedure: usp_GetLowStockAlerts
-- Purpose: Get real-time alerts for low stock and out-of-stock products
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetLowStockAlerts]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_GetLowStockAlerts]
GO

CREATE PROCEDURE [dbo].[usp_GetLowStockAlerts]
    @Threshold INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        Id, Name, Category, Price, StockQuantity, LowStockThreshold,
        CASE 
            WHEN StockQuantity = 0 THEN 'CRITICAL: Out of Stock'
            WHEN StockQuantity = 1 THEN 'CRITICAL: Only 1 left'
            WHEN StockQuantity <= LowStockThreshold THEN 'WARNING: Low Stock'
            ELSE 'OK'
        END AS AlertLevel,
        CASE 
            WHEN StockQuantity = 0 THEN 3
            WHEN StockQuantity = 1 THEN 2
            WHEN StockQuantity <= LowStockThreshold THEN 1
            ELSE 0
        END AS AlertPriority
    FROM Products
    WHERE IsVisible = 1 
        AND (StockQuantity = 0 OR StockQuantity <= ISNULL(@Threshold, LowStockThreshold))
    ORDER BY 
        CASE WHEN StockQuantity = 0 THEN 1 WHEN StockQuantity = 1 THEN 2 ELSE 3 END,
        StockQuantity ASC;
END
GO

PRINT 'Stored Procedure usp_GetLowStockAlerts created successfully!';