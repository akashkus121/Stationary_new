-- =============================================
-- Migration: Add PaymentMethod to Orders table
-- Purpose: Add payment method tracking for orders
-- =============================================

-- Add PaymentMethod column to Orders table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Orders]') AND name = 'PaymentMethod')
BEGIN
    ALTER TABLE [dbo].[Orders] 
    ADD [PaymentMethod] NVARCHAR(50) NOT NULL DEFAULT 'cash';
    
    PRINT 'PaymentMethod column added to Orders table successfully!';
END
ELSE
BEGIN
    PRINT 'PaymentMethod column already exists in Orders table.';
END

-- Update existing orders to have 'cash' as default payment method
UPDATE [dbo].[Orders] 
SET [PaymentMethod] = 'cash' 
WHERE [PaymentMethod] IS NULL OR [PaymentMethod] = '';

PRINT 'Migration completed successfully!';
