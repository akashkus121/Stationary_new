# Stock Management Stored Procedures

This directory contains stored procedures for advanced stock management operations in the Stationary application.

## 📁 **File Structure**

```
StoredProcedures/
├── 00_InstallAllStoredProcedures.sql    # Master installation script
├── 01_StockAlertSummary.sql             # Stock overview and counts
├── 02_ProductsByStockStatus.sql         # Product filtering by stock status
├── 03_BulkUpdateStock.sql               # Bulk stock updates
├── 04_LowStockAlerts.sql                # Low stock alerts and warnings
├── 05_UpdateProductVisibility.sql       # Product visibility management
└── README.md                             # This file
```

## 🚀 **Installation**

### **Option 1: Master Script (Recommended)**
1. Run `00_InstallAllStoredProcedures.sql` first
2. This will create the required table type
3. Then run the individual SP files in order

### **Option 2: Manual Installation**
1. Create the table type from `00_InstallAllStoredProcedures.sql`
2. Run each stored procedure file in numerical order:
   - `01_StockAlertSummary.sql`
   - `02_ProductsByStockStatus.sql`
   - `03_BulkUpdateStock.sql`
   - `04_LowStockAlerts.sql`
   - `05_UpdateProductVisibility.sql`

## 📊 **Stored Procedures Overview**

### **1. usp_GetStockAlertSummary**
- **Purpose**: Get comprehensive stock overview
- **Returns**: Counts of products by stock status
- **Use Case**: Admin dashboard, stock reports

### **2. usp_GetProductsByStockStatus**
- **Purpose**: Filter products by stock availability
- **Parameters**: 
  - `@StockStatus`: 'available', 'lowstock', 'outofstock', 'all'
  - `@Category`: Optional category filter
  - `@SearchTerm`: Optional search term
  - `@Page`, `@PageSize`: Pagination
- **Use Case**: Product listings, search results

### **3. usp_BulkUpdateStock**
- **Purpose**: Update multiple products' stock levels efficiently
- **Parameters**: `@Updates` (table-valued parameter)
- **Use Case**: Bulk inventory updates, import operations

### **4. usp_GetLowStockAlerts**
- **Purpose**: Get real-time stock alerts
- **Parameters**: `@Threshold` (optional custom threshold)
- **Returns**: Products with stock warnings
- **Use Case**: Stock monitoring, notification systems

### **5. usp_UpdateProductVisibility**
- **Purpose**: Automatically manage product visibility
- **Parameters**: `@AutoHideOutOfStock` (boolean)
- **Use Case**: Inventory management, user experience

## 🔧 **Usage Examples**

### **Get Stock Summary**
```sql
EXEC usp_GetStockAlertSummary;
```

### **Get Available Products**
```sql
EXEC usp_GetProductsByStockStatus 
    @StockStatus = 'available',
    @Page = 1,
    @PageSize = 20;
```

### **Get Low Stock Alerts**
```sql
EXEC usp_GetLowStockAlerts @Threshold = 5;
```

### **Update Product Visibility**
```sql
EXEC usp_UpdateProductVisibility @AutoHideOutOfStock = 1;
```

## 📈 **Performance Benefits**

- **Reduced Network Traffic**: Single SP calls vs multiple EF queries
- **Optimized Execution Plans**: SQL Server can optimize SP execution
- **Bulk Operations**: Efficient handling of large datasets
- **Indexed Queries**: Better use of database indexes

## ⚠️ **Prerequisites**

1. **Database Schema**: Run the migration script first
2. **Indexes**: Install performance indexes
3. **Permissions**: Ensure proper database permissions
4. **SQL Server**: Compatible with SQL Server 2016+

## 🔄 **Integration with Application**

The application includes a `ProductServiceWithSP` class that can optionally use these stored procedures:

```csharp
// Register with stored procedures enabled
services.AddScoped<IProductService, ProductServiceWithSP>(provider => 
    new ProductServiceWithSP(provider.GetRequiredService<ApplicationDbContext>(), true));
```

## 📝 **Maintenance**

- **Regular Updates**: Keep SPs in sync with schema changes
- **Performance Monitoring**: Monitor SP execution times
- **Index Maintenance**: Regular index rebuilds and statistics updates
- **Backup**: Include SPs in database backup strategy

## 🆘 **Troubleshooting**

### **Common Issues**
1. **Missing Table Type**: Run the master script first
2. **Permission Errors**: Check database user permissions
3. **Performance Issues**: Verify indexes are created
4. **Parameter Errors**: Check parameter types and names

### **Fallback**
If stored procedures fail, the application automatically falls back to Entity Framework Core queries.

## 📞 **Support**

For issues or questions about these stored procedures:
1. Check the application logs
2. Verify database connectivity
3. Test SPs directly in SQL Server Management Studio
4. Review the application's fallback mechanisms