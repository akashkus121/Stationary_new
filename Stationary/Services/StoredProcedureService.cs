using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;

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

        public StoredProcedureService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<StockAlertSummary> GetStockAlertSummaryAsync()
        {
            var products = await _db.Products.AsNoTracking().ToListAsync();
            return new StockAlertSummary
            {
                TotalProducts = products.Count,
                OutOfStockProducts = products.Count(p => p.StockQuantity <= 0),
                LowStockProducts = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold),
                InStockProducts = products.Count(p => p.StockQuantity > p.LowStockThreshold),
                VisibleProducts = products.Count(p => p.IsVisible),
                HiddenProducts = products.Count(p => !p.IsVisible)
            };
        }

        public async Task<IEnumerable<Product>> GetProductsByStockStatusAsync(string stockStatus, string? category = null, string? searchTerm = null, int page = 1, int pageSize = 20)
        {
            var query = _db.Products.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || p.Category.ToLower().Contains(term));
            }

            switch (stockStatus.ToLower())
            {
                case "outofstock":
                    query = query.Where(p => p.StockQuantity <= 0);
                    break;
                case "lowstock":
                    query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold);
                    break;
                case "instock":
                    query = query.Where(p => p.StockQuantity > p.LowStockThreshold);
                    break;
            }

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> BulkUpdateStockAsync(List<StockUpdateModel> stockUpdates)
        {
            if (stockUpdates == null || stockUpdates.Count == 0) return true;

            var productIds = stockUpdates.Select(u => u.ProductId).ToList();
            var products = await _db.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            foreach (var update in stockUpdates)
            {
                var prod = products.FirstOrDefault(p => p.Id == update.ProductId);
                if (prod != null)
                {
                    prod.StockQuantity = update.NewStockQuantity;
                    prod.LowStockThreshold = update.NewLowStockThreshold;
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Product>> GetLowStockAlertsAsync()
        {
            return await _db.Products
                .AsNoTracking()
                .Where(p => p.StockQuantity <= p.LowStockThreshold)
                .ToListAsync();
        }

        public async Task<bool> UpdateProductVisibilityAsync(bool autoHideOutOfStock = true)
        {
            if (autoHideOutOfStock)
            {
                var outOfStock = await _db.Products.Where(p => p.StockQuantity <= 0 && p.IsVisible).ToListAsync();
                foreach (var p in outOfStock)
                {
                    p.IsVisible = false;
                }
                await _db.SaveChangesAsync();
            }
            return true;
        }

        public async Task<IEnumerable<Cart>> GetCartItemsAsync(int userId)
        {
            return await _db.Carts
                .AsNoTracking()
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<int> CreateOrderAsync(int userId, decimal subtotal, decimal taxAmount, decimal totalAmount, string paymentMethod = "cash", string orderStatus = "Pending", string? notes = null)
        {
            var order = new Order
            {
                UserId = userId,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                PaymentMethod = paymentMethod,
                Date = DateTime.UtcNow
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            return order.Id;
        }

        public async Task<IEnumerable<Order>> GetOrderHistoryAsync(int userId, int page = 1, int pageSize = 20)
        {
            return await _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderDetailsAsync(int orderId, int userId)
        {
            return await _db.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        }

        public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _db.Orders.AsNoTracking().AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(o => o.Date >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(o => o.Date <= endDate.Value);
            }

            var orders = await query.ToListAsync();
            var totalOrders = orders.Count;
            var totalRevenue = orders.Sum(o => o.TotalAmount);
            var totalSubtotal = orders.Sum(o => o.Subtotal);
            var totalTax = orders.Sum(o => o.TaxAmount);
            var uniqueCustomers = orders.Select(o => o.UserId).Distinct().Count();

            return new SalesReportViewModel
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalSubtotal = totalSubtotal,
                TotalTax = totalTax,
                AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
                UniqueCustomers = uniqueCustomers,
                TotalSalesAmount = totalRevenue
            };
        }
    }
}
