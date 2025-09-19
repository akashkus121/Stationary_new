# Database Installation Guide

## 🚀 **Quick Start (Recommended)**

1. **Open SQL Server Management Studio (SSMS)**
2. **Connect to your SQL Server instance**
3. **Open and run**: `00_MasterInstallation.sql`
4. **Done!** Your database is ready

## 📋 **Step-by-Step Installation**

### **Option 1: Master Script (Easiest)**
```sql
-- Just run this one file
:r "00_MasterInstallation.sql"
```

### **Option 2: Individual Scripts**
```sql
-- Run these in order:
1. 01_CreateDatabase.sql      -- Creates database and tables
2. 02_CreateIndexes.sql       -- Creates performance indexes
3. 06_AddProductStockFields.sql -- Adds stock-related columns
4. Install stored procedures   -- From StoredProcedures folder
```

## 🔧 **What Gets Created**

### **Database**
- **Name**: `StationaryDB`
- **Tables**: Users, Products, Carts, Orders, OrderItems
- **Indexes**: Performance indexes for stock management
- **Stored Procedures**: 5 stock management procedures
- **Stock Fields**: StockQuantity, LowStockThreshold, IsVisible

### **Sample Data**
- **Admin User**: admin / admin123
- **Regular User**: user / user123
- **Sample Products**: 5 products with different stock levels

## ⚠️ **Prerequisites**

- SQL Server 2016 or later
- SQL Server Management Studio (SSMS)
- Database creation permissions

## 🐛 **Troubleshooting**

### **Common Errors**

1. **"Database already exists"**
   - This is normal, just continue

2. **"Permission denied"**
   - Check your SQL Server login permissions
   - Ensure you can create databases

3. **"File not found"**
   - Make sure all script files are in the same folder
   - Use the master script instead

### **If Master Script Fails**

Run the individual scripts manually in this order:
1. `01_CreateDatabase.sql`
2. `02_CreateIndexes.sql`
3. Each stored procedure file from the StoredProcedures folder

## 📊 **Verification**

After installation, run these queries to verify:

```sql
-- Check database exists
SELECT name FROM sys.databases WHERE name = 'StationaryDB';

-- Check tables exist
USE StationaryDB;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;

-- Check stored procedures exist
SELECT name FROM sys.procedures WHERE name LIKE 'usp_%';

-- Check sample data
SELECT * FROM Users;
SELECT * FROM Products;
```

## 🔄 **Updating Existing Database**

If you already have a database:

1. **Backup your existing data**
2. **Run only the new scripts**:
   - `02_CreateIndexes.sql`
   - `06_AddProductStockFields.sql` (adds stock fields)
   - Stored procedure files
3. **Test thoroughly**

### **Quick Stock Fields Update**
If you only need to add the stock fields to existing products:
```sql
-- Run this standalone script
:r "06_AddProductStockFields_Standalone.sql"
```

## 📞 **Need Help?**

1. Check the error messages in SSMS
2. Verify all files are in the correct folders
3. Ensure SQL Server version compatibility
4. Check database permissions

## 🎯 **Next Steps**

After successful installation:
1. Update your application's connection string
2. Test the application
3. Monitor performance with the new indexes
4. Use the stored procedures for better performance