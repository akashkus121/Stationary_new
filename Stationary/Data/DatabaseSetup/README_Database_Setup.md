# Database Setup Guide for Stationary Application

This guide explains how to set up the complete database with tables and stored procedures for the Stationary application.

## 📁 Files Overview

### Database Scripts
- `08_CompleteDatabaseScript.sql` - **Main script** - Complete database setup with all tables and stored procedures
- `00_MasterInstallation.sql` - Master installation script (references other files)
- `01_CreateDatabase.sql` - Basic database and table creation
- `02_CreateIndexes.sql` - Performance indexes
- `06_AddProductStockFields.sql` - Product stock fields migration
- `07_AddPaymentMethodToOrders.sql` - Payment method migration

### Stored Procedures
- `01_StockAlertSummary.sql` - Get stock alert summary
- `02_ProductsByStockStatus.sql` - Get products with filtering
- `03_BulkUpdateStock.sql` - Bulk update stock quantities
- `04_LowStockAlerts.sql` - Get low stock alerts
- `05_UpdateProductVisibility.sql` - Update product visibility

## 🚀 Quick Setup

### Option 1: Complete Setup (Recommended)
Run the complete database script that includes everything:

```sql
-- Run this script in SQL Server Management Studio or Azure Data Studio
:r "08_CompleteDatabaseScript.sql"
```

### Option 2: Step-by-Step Setup
If you prefer to run scripts individually:

1. **Create Database and Tables:**
   ```sql
   :r "01_CreateDatabase.sql"
   ```

2. **Add Performance Indexes:**
   ```sql
   :r "02_CreateIndexes.sql"
   ```

3. **Add Stock Fields:**
   ```sql
   :r "06_AddProductStockFields.sql"
   ```

4. **Add Payment Method:**
   ```sql
   :r "07_AddPaymentMethodToOrders.sql"
   ```

5. **Install Stored Procedures:**
   ```sql
   :r "../StoredProcedures/01_StockAlertSummary.sql"
   :r "../StoredProcedures/02_ProductsByStockStatus.sql"
   :r "../StoredProcedures/03_BulkUpdateStock.sql"
   :r "../StoredProcedures/04_LowStockAlerts.sql"
   :r "../StoredProcedures/05_UpdateProductVisibility.sql"
   ```

## 📊 Database Schema

### Tables Created

#### Users
- `Id` (Primary Key)
- `Username` (Unique)
- `Password`
- `Role` (Admin/User)
- `Email`
- `Phone`
- `CreatedDate`
- `IsActive`

#### Products
- `Id` (Primary Key)
- `Name`
- `Category`
- `Price`
- `ImagePath`
- `StockQuantity`
- `LowStockThreshold`
- `IsVisible`
- `Description`
- `CreatedDate`
- `UpdatedDate`
- `IsActive`

#### Carts
- `Id` (Primary Key)
- `UserId` (Foreign Key)
- `ProductId` (Foreign Key)
- `Quantity`
- `AddedDate`
- `UpdatedDate`

#### Orders
- `Id` (Primary Key)
- `UserId` (Foreign Key)
- `TotalAmount`
- `Subtotal`
- `TaxAmount`
- `PaymentMethod` (cash/upi)
- `OrderStatus`
- `Date`
- `UpdatedDate`
- `Notes`

#### OrderItems
- `Id` (Primary Key)
- `OrderId` (Foreign Key)
- `ProductId` (Foreign Key)
- `ProductName`
- `Quantity`
- `Price`
- `TotalPrice`

## 🔧 Stored Procedures

### 1. Stock Management
- `usp_GetStockAlertSummary` - Get overview of stock status
- `usp_GetProductsByStockStatus` - Get products with filtering and pagination
- `usp_BulkUpdateStock` - Update multiple products' stock at once
- `usp_GetLowStockAlerts` - Get products with low stock
- `usp_UpdateProductVisibility` - Auto-hide/show products based on stock

### 2. Cart Management
- `usp_GetCartItems` - Get user's cart with product details

### 3. Order Management
- `usp_CreateOrder` - Create new order and clear cart
- `usp_GetOrderHistory` - Get user's order history
- `usp_GetOrderDetails` - Get detailed order information

### 4. Reporting
- `usp_GetSalesReport` - Get sales analytics and top products

## 💻 Backend Integration

### Service Registration
The stored procedure service is already registered in `Program.cs`:

```csharp
builder.Services.AddScoped<IStoredProcedureService, StoredProcedureService>();
```

### Using Stored Procedures in Controllers

#### Example 1: Get Stock Alerts
```csharp
[HttpGet("stock-alerts")]
public async Task<IActionResult> GetStockAlerts()
{
    var summary = await _spService.GetStockAlertSummaryAsync();
    return Ok(summary);
}
```

#### Example 2: Bulk Update Stock
```csharp
[HttpPost("bulk-update-stock")]
public async Task<IActionResult> BulkUpdateStock([FromBody] List<StockUpdateModel> updates)
{
    var success = await _spService.BulkUpdateStockAsync(updates);
    return Ok(new { success, updatedCount = updates.Count });
}
```

#### Example 3: Create Order
```csharp
[HttpPost("create-order")]
public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
{
    var orderId = await _spService.CreateOrderAsync(
        request.UserId,
        request.Subtotal,
        request.TaxAmount,
        request.TotalAmount,
        request.PaymentMethod ?? "cash"
    );
    return Ok(new { orderId });
}
```

### API Endpoints

The `StoredProcedureController` provides REST API endpoints:

- `GET /api/storedprocedure/stock-alerts` - Get stock summary
- `GET /api/storedprocedure/products` - Get products with filtering
- `POST /api/storedprocedure/bulk-update-stock` - Bulk update stock
- `GET /api/storedprocedure/low-stock-alerts` - Get low stock alerts
- `POST /api/storedprocedure/update-visibility` - Update product visibility
- `GET /api/storedprocedure/cart/{userId}` - Get user's cart
- `POST /api/storedprocedure/create-order` - Create new order
- `GET /api/storedprocedure/orders/{userId}` - Get order history
- `GET /api/storedprocedure/order/{orderId}/{userId}` - Get order details
- `GET /api/storedprocedure/sales-report` - Get sales report

## 🔍 Sample Data

The complete script includes sample data:

### Default Users
- **Admin**: `admin` / `admin123`
- **User**: `user` / `user123`
- **Akash**: `akash` / `akash123`

### Sample Products
- Blue Ballpoint Pen - $1.99
- A4 Notebook 200 Pages - $5.99
- Heavy Duty Stapler - $12.99
- Paper Clips Box (100) - $2.99
- And more...

## 🚨 Important Notes

1. **Connection String**: Ensure your `appsettings.json` has the correct connection string
2. **Permissions**: The database user needs CREATE, ALTER, and EXECUTE permissions
3. **Backup**: Always backup your database before running migration scripts
4. **Testing**: Test stored procedures in a development environment first

## 🔧 Troubleshooting

### Common Issues

1. **Connection String Error**
   - Check `appsettings.json` for correct connection string
   - Ensure SQL Server is running and accessible

2. **Permission Denied**
   - Grant necessary permissions to the database user
   - Run scripts as a user with sufficient privileges

3. **Stored Procedure Not Found**
   - Ensure all stored procedure scripts were executed
   - Check if procedures exist in the database

4. **Table Type Error**
   - Ensure `StockUpdateTableType` was created before running bulk update procedures

### Verification Queries

Check if everything was created successfully:

```sql
-- Check tables
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'

-- Check stored procedures
SELECT ROUTINE_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE ROUTINE_TYPE = 'PROCEDURE'

-- Check indexes
SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('Products')
```

## 📈 Performance Tips

1. **Indexes**: All necessary indexes are created for optimal performance
2. **Pagination**: Use pagination for large result sets
3. **Connection Pooling**: The service uses proper connection management
4. **Async Operations**: All database operations are asynchronous

## 🔄 Migration Strategy

For production deployments:

1. **Backup** the existing database
2. **Test** scripts in a staging environment
3. **Run** migration scripts during maintenance windows
4. **Verify** all procedures and data integrity
5. **Update** application configuration if needed

---

**Need Help?** Check the application logs for detailed error messages and ensure all prerequisites are met.
