-- =============================================
-- Database Setup Script for Stationary Application
-- Purpose: Create database and tables for stock management
-- =============================================

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

-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Users](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Username] [nvarchar](50) NOT NULL,
        [Password] [nvarchar](100) NOT NULL,
        [Role] [nvarchar](20) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC)
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
        [Date] [datetime] NOT NULL,
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
        CONSTRAINT [PK_OrderItems] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    PRINT 'OrderItems table created successfully!';
END
ELSE
BEGIN
    PRINT 'OrderItems table already exists.';
END

-- Add foreign key constraints
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Carts_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Carts]'))
BEGIN
    ALTER TABLE [dbo].[Carts] ADD CONSTRAINT [FK_Carts_Users] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([Id]);
    PRINT 'Foreign key FK_Carts_Users added successfully!';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Carts_Products]') AND parent_object_id = OBJECT_ID(N'[dbo].[Carts]'))
BEGIN
    ALTER TABLE [dbo].[Carts] ADD CONSTRAINT [FK_Carts_Products] FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products] ([Id]);
    PRINT 'Foreign key FK_Carts_Products added successfully!';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Orders_Users]') AND parent_object_id = OBJECT_ID(N'[dbo].[Orders]'))
BEGIN
    ALTER TABLE [dbo].[Orders] ADD CONSTRAINT [FK_Orders_Users] FOREIGN KEY([UserId]) REFERENCES [dbo].[Users] ([Id]);
    PRINT 'Foreign key FK_Orders_Users added successfully!';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderItems_Orders]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderItems]'))
BEGIN
    ALTER TABLE [dbo].[OrderItems] ADD CONSTRAINT [FK_OrderItems_Orders] FOREIGN KEY([OrderId]) REFERENCES [dbo].[Orders] ([Id]);
    PRINT 'Foreign key FK_OrderItems_Orders added successfully!';
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_OrderItems_Products]') AND parent_object_id = OBJECT_ID(N'[dbo].[OrderItems]'))
BEGIN
    ALTER TABLE [dbo].[OrderItems] ADD CONSTRAINT [FK_OrderItems_Products] FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products] ([Id]);
    PRINT 'Foreign key FK_OrderItems_Products added successfully!';
END

PRINT 'Database setup completed successfully!';
GO