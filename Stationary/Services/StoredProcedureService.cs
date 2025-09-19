using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;
using System.Data;

namespace Stationary.Services
{
    public interface IStoredProcedureService
    {
        Task<StockAlertSummary> GetStockAlertSummaryAsync();
        Task<IEnumerable<Product>> GetProductsByStockStatusAsync(string stockStatus, string? category = null, string? searchTerm = null, int page = 1, int pageSize = 20);
        Task<bool> BulkUpdateStockAsync(List<StockUpdateModel> stockUpdates);
        Task<IEnumerable<Product>> GetLowStockAlertsAsync();
        Task<bool> UpdateProductVisibilityAsync(bool autoHideOutOfStock = true);
        Task<IEnumerable<Cart>> GetCartItemsAsync(int userId);
        Task<int> CreateOrderAsync(int userId, decimal subtotal, decimal taxAmount, decimal totalAmount, string paymentMethod = "cash", string orderStatus = "Pending", string? notes = null);
        Task<IEnumerable<Order>> GetOrderHistoryAsync(int userId, int page = 1, int pageSize = 20);
        Task<Order?> GetOrderDetailsAsync(int orderId, int userId);
        Task<SalesReportViewModel> GetSalesReportAsync(DateTime? startDate = null, DateTime? endDate = null);
    }

    public class StoredProcedureService : IStoredProcedureService
    {
        private readonly ApplicationDbContext _db;
        private readonly string _connectionString;

        public StoredProcedureService(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? 
                               _db.Database.GetConnectionString() ?? 
                               throw new InvalidOperationException("Connection string not found");
        }

        public async Task<StockAlertSummary> GetStockAlertSummaryAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_GetStockAlertSummary", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new StockAlertSummary
                {
                    TotalProducts = reader.GetInt32("TotalProducts"),
                    OutOfStockProducts = reader.GetInt32("OutOfStock"),
                    LowStockProducts = reader.GetInt32("LowStock"),
                    InStockProducts = reader.GetInt32("InStock"),
                    VisibleProducts = reader.GetInt32("VisibleProducts"),
                    HiddenProducts = reader.GetInt32("HiddenProducts")
                };
            }

            return new StockAlertSummary();
        }

        public async Task<IEnumerable<Product>> GetProductsByStockStatusAsync(string stockStatus, string? category = null, string? searchTerm = null, int page = 1, int pageSize = 20)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_GetProductsByStockStatus", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@StockStatus", stockStatus);
            command.Parameters.AddWithValue("@Category", (object?)category ?? DBNull.Value);
            command.Parameters.AddWithValue("@SearchTerm", (object?)searchTerm ?? DBNull.Value);
            command.Parameters.AddWithValue("@Page", page);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var products = new List<Product>();
            while (await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Category = reader.GetString("Category"),
                    Price = reader.GetDecimal("Price"),
                    ImagePath = reader.IsDBNull("ImagePath") ? null : reader.GetString("ImagePath"),
                    StockQuantity = reader.GetInt32("StockQuantity"),
                    LowStockThreshold = reader.GetInt32("LowStockThreshold"),
                    IsVisible = reader.GetBoolean("IsVisible"),
                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description"),
                    CreatedDate = reader.GetDateTime("CreatedDate")
                });
            }

            return products;
        }

        public async Task<bool> BulkUpdateStockAsync(List<StockUpdateModel> stockUpdates)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_BulkUpdateStock", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Create DataTable for table-valued parameter
            var stockUpdateTable = new DataTable();
            stockUpdateTable.Columns.Add("ProductId", typeof(int));
            stockUpdateTable.Columns.Add("NewStockQuantity", typeof(int));
            stockUpdateTable.Columns.Add("NewLowStockThreshold", typeof(int));
            stockUpdateTable.Columns.Add("ProductName", typeof(string));
            stockUpdateTable.Columns.Add("CurrentStock", typeof(int));
            stockUpdateTable.Columns.Add("CurrentLowStockThreshold", typeof(int));

            foreach (var update in stockUpdates)
            {
                stockUpdateTable.Rows.Add(
                    update.ProductId,
                    update.NewStockQuantity,
                    update.NewLowStockThreshold,
                    update.ProductName,
                    update.CurrentStock,
                    update.CurrentLowStockThreshold
                );
            }

            var parameter = command.Parameters.AddWithValue("@StockUpdates", stockUpdateTable);
            parameter.SqlDbType = SqlDbType.Structured;
            parameter.TypeName = "dbo.StockUpdateTableType";

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var updatedProducts = reader.GetInt32("UpdatedProducts");
                var errors = reader.GetInt32("Errors");
                return errors == 0;
            }

            return false;
        }

        public async Task<IEnumerable<Product>> GetLowStockAlertsAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_GetLowStockAlerts", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var products = new List<Product>();
            while (await reader.ReadAsync())
            {
                products.Add(new Product
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Category = reader.GetString("Category"),
                    Price = reader.GetDecimal("Price"),
                    StockQuantity = reader.GetInt32("StockQuantity"),
                    LowStockThreshold = reader.GetInt32("LowStockThreshold")
                });
            }

            return products;
        }

        public async Task<bool> UpdateProductVisibilityAsync(bool autoHideOutOfStock = true)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_UpdateProductVisibility", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@AutoHideOutOfStock", autoHideOutOfStock);

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }

        public async Task<IEnumerable<Cart>> GetCartItemsAsync(int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_GetCartItems", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var cartItems = new List<Cart>();
            while (await reader.ReadAsync())
            {
                cartItems.Add(new Cart
                {
                    Id = reader.GetInt32("CartId"),
                    UserId = userId,
                    ProductId = reader.GetInt32("ProductId"),
                    Quantity = reader.GetInt32("Quantity"),
                    AddedDate = reader.GetDateTime("AddedDate"),
                    Product = new Product
                    {
                        Id = reader.GetInt32("ProductId"),
                        Name = reader.GetString("Name"),
                        Category = reader.GetString("Category"),
                        Price = reader.GetDecimal("Price"),
                        ImagePath = reader.IsDBNull("ImagePath") ? null : reader.GetString("ImagePath"),
                        StockQuantity = reader.GetInt32("StockQuantity"),
                        LowStockThreshold = reader.GetInt32("LowStockThreshold"),
                        IsVisible = reader.GetBoolean("IsVisible")
                    }
                });
            }

            return cartItems;
        }

        public async Task<int> CreateOrderAsync(int userId, decimal subtotal, decimal taxAmount, decimal totalAmount, string paymentMethod = "cash", string orderStatus = "Pending", string? notes = null)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_CreateOrder", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Subtotal", subtotal);
            command.Parameters.AddWithValue("@TaxAmount", taxAmount);
            command.Parameters.AddWithValue("@TotalAmount", totalAmount);
            command.Parameters.AddWithValue("@PaymentMethod", paymentMethod);
            command.Parameters.AddWithValue("@OrderStatus", orderStatus);
            command.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);

            var orderIdParameter = command.Parameters.Add("@OrderId", SqlDbType.Int);
            orderIdParameter.Direction = ParameterDirection.Output;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            return (int)orderIdParameter.Value;
        }

        public async Task<IEnumerable<Order>> GetOrderHistoryAsync(int userId, int page = 1, int pageSize = 20)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_GetOrderHistory", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Page", page);
            command.Parameters.AddWithValue("@PageSize", pageSize);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var orders = new List<Order>();
            while (await reader.ReadAsync())
            {
                orders.Add(new Order
                {
                    Id = reader.GetInt32("Id"),
                    UserId = userId,
                    TotalAmount = reader.GetDecimal("TotalAmount"),
                    Date = reader.GetDateTime("Date"),
                    PaymentMethod = reader.GetString("PaymentMethod")
                });
            }

            return orders;
        }

        public async Task<Order?> GetOrderDetailsAsync(int orderId, int userId)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_GetOrderDetails", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@OrderId", orderId);
            command.Parameters.AddWithValue("@UserId", userId);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var order = new Order
                {
                    Id = reader.GetInt32("Id"),
                    UserId = userId,
                    TotalAmount = reader.GetDecimal("TotalAmount"),
                    Date = reader.GetDateTime("Date"),
                    PaymentMethod = reader.GetString("PaymentMethod")
                };

                // Read order items if there are multiple result sets
                if (reader.NextResult())
                {
                    var orderItems = new List<OrderItem>();
                    while (await reader.ReadAsync())
                    {
                        orderItems.Add(new OrderItem
                        {
                            Id = reader.GetInt32("Id"),
                            OrderId = orderId,
                            ProductId = reader.GetInt32("ProductId"),
                            ProductName = reader.GetString("ProductName"),
                            Quantity = reader.GetInt32("Quantity"),
                            Price = reader.GetDecimal("Price")
                        });
                    }
                    order.OrderItems = orderItems;
                }

                return order;
            }

            return null;
        }

        public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("usp_GetSalesReport", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@StartDate", (object?)startDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", (object?)endDate ?? DBNull.Value);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();
            
            var report = new SalesReportViewModel();
            
            if (await reader.ReadAsync())
            {
                report.TotalOrders = reader.GetInt32("TotalOrders");
                report.TotalRevenue = reader.GetDecimal("TotalRevenue");
                report.TotalSubtotal = reader.GetDecimal("TotalSubtotal");
                report.TotalTax = reader.GetDecimal("TotalTax");
                report.AverageOrderValue = reader.GetDecimal("AverageOrderValue");
                report.UniqueCustomers = reader.GetInt32("UniqueCustomers");
                
                // Set legacy properties for backward compatibility
                report.TotalSalesAmount = report.TotalRevenue;
            }

            // Read top selling products if there are multiple result sets
            if (reader.NextResult())
            {
                var topProducts = new List<dynamic>();
                while (await reader.ReadAsync())
                {
                    topProducts.Add(new
                    {
                        ProductName = reader.GetString("ProductName"),
                        TotalQuantity = reader.GetInt32("TotalQuantity"),
                        TotalRevenue = reader.GetDecimal("TotalRevenue"),
                        OrderCount = reader.GetInt32("OrderCount")
                    });
                }
                report.TopSellingProducts = topProducts;
            }

            return report;
        }
    }
}
