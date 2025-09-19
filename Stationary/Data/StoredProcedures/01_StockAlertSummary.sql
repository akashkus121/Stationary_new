-- =============================================
-- Stored Procedure: usp_GetStockAlertSummary
-- Purpose: Get comprehensive stock overview with counts and percentages
-- =============================================

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetStockAlertSummary]') AND type in (N'P', N'PC'))
DROP PROCEDURE [dbo].[usp_GetStockAlertSummary]
GO

CREATE PROCEDURE [dbo].[usp_GetStockAlertSummary]
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        COUNT(*) AS TotalProducts,
        SUM(CASE WHEN StockQuantity > LowStockThreshold THEN 1 ELSE 0 END) AS InStockProducts,
        SUM(CASE WHEN StockQuantity > 0 AND StockQuantity <= LowStockThreshold THEN 1 ELSE 0 END) AS LowStockProducts,
        SUM(CASE WHEN StockQuantity <= 0 THEN 1 ELSE 0 END) AS OutOfStockProducts,
        SUM(CASE WHEN StockQuantity = 1 THEN 1 ELSE 0 END) AS CriticalStockProducts
    FROM Products
    WHERE IsVisible = 1;
END
GO

PRINT 'Stored Procedure usp_GetStockAlertSummary created successfully!';