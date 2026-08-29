-- =============================================================================
-- Supabase (PostgreSQL) Database Initialization Script
-- Project: Stationary Management System
-- Description: Creates tables, constraints, indexes, and initial admin seed.
-- =============================================================================

-- Enable uuid-ossp extension if needed
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 1. Users Table
CREATE TABLE IF NOT EXISTS "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(100) NOT NULL UNIQUE,
    "Password" VARCHAR(500) NOT NULL,
    "Role" VARCHAR(50) NOT NULL DEFAULT 'User',
    "RefreshToken" TEXT NULL,
    "RefreshTokenExpiryTime" TIMESTAMPTZ NULL
);

-- 2. Products Table
CREATE TABLE IF NOT EXISTS "Products" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(200) NOT NULL,
    "Category" VARCHAR(100) NOT NULL,
    "Price" NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    "StockQuantity" INTEGER NOT NULL DEFAULT 0,
    "LowStockThreshold" INTEGER NOT NULL DEFAULT 5,
    "ImagePath" VARCHAR(500) NULL,
    "Description" TEXT NULL,
    "IsVisible" BOOLEAN NOT NULL DEFAULT TRUE,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "UpdatedDate" TIMESTAMPTZ NULL,
    "AdminId" INTEGER NULL,
    "AdminUsername" VARCHAR(100) NULL
);

-- 3. Carts Table
CREATE TABLE IF NOT EXISTS "Carts" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "ProductId" INTEGER NOT NULL,
    "Quantity" INTEGER NOT NULL DEFAULT 1,
    "AddedDate" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT "FK_Carts_Products_ProductId" FOREIGN KEY ("ProductId") 
        REFERENCES "Products" ("Id") ON DELETE CASCADE
);

-- 4. Orders Table
CREATE TABLE IF NOT EXISTS "Orders" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "Date" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "TotalAmount" NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    "Subtotal" NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    "TaxAmount" NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    "PaymentMethod" VARCHAR(50) NOT NULL DEFAULT 'cash',
    "OrderStatus" VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "UpdatedDate" TIMESTAMPTZ NULL,
    "Notes" TEXT NULL
);

-- 5. OrderItems Table
CREATE TABLE IF NOT EXISTS "OrderItems" (
    "Id" SERIAL PRIMARY KEY,
    "OrderId" INTEGER NOT NULL,
    "ProductId" INTEGER NOT NULL,
    "ProductName" VARCHAR(200) NOT NULL,
    "Price" NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    "Quantity" INTEGER NOT NULL DEFAULT 1,
    "TotalPrice" NUMERIC(18, 2) NOT NULL DEFAULT 0.00,
    "AdminId" INTEGER NULL,
    CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") 
        REFERENCES "Orders" ("Id") ON DELETE CASCADE
);

-- Indexes for ultra-fast queries & Redis sync
CREATE INDEX IF NOT EXISTS "IX_Products_Category" ON "Products" ("Category");
CREATE INDEX IF NOT EXISTS "IX_Products_IsVisible" ON "Products" ("IsVisible");
CREATE INDEX IF NOT EXISTS "IX_Products_StockQuantity" ON "Products" ("StockQuantity");
CREATE INDEX IF NOT EXISTS "IX_Carts_UserId" ON "Carts" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Orders_UserId" ON "Orders" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");

-- Initial Admin User Seed (Username: akash, default password: 12345)
-- Note: When the backend runs, ASP.NET Identity PasswordHasher will also ensure the password hash matches.
INSERT INTO "Users" ("Username", "Password", "Role")
VALUES ('akash', '12345', 'Admin')
ON CONFLICT ("Username") DO NOTHING;
