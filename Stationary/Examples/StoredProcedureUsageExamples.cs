//using Microsoft.AspNetCore.Mvc;
//using Stationary.Services;
//using Stationary.Models;

//namespace Stationary.Examples
//{
//    /// <summary>
//    /// Examples of how to use stored procedures in your controllers
//    /// </summary>
//    public class StoredProcedureUsageExamples
//    {
//        private readonly IStoredProcedureService _spService;

//        public StoredProcedureUsageExamples(IStoredProcedureService spService)
//        {
//            _spService = spService;
//        }

//        /// <summary>
//        /// Example 1: Get stock alerts for admin dashboard
//        /// </summary>
//        public async Task<IActionResult> GetAdminDashboardData()
//        {
//            try
//            {
//                // Get stock summary
//                var stockSummary = await _spService.GetStockAlertSummaryAsync();
                
//                // Get low stock alerts
//                var lowStockAlerts = await _spService.GetLowStockAlertsAsync();
                
//                // Get recent sales report
//                var salesReport = await _spService.GetSalesReportAsync(
//                    DateTime.Today.AddDays(-30), 
//                    DateTime.Today
//                );

//                return new JsonResult(new
//                {
//                    StockSummary = stockSummary,
//                    LowStockAlerts = lowStockAlerts,
//                    SalesReport = salesReport
//                });
//            }
//            catch (Exception ex)
//            {
//                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
//            }
//        }

//        /// <summary>
//        /// Example 2: Bulk update stock from CSV upload
//        /// </summary>
//        public async Task<IActionResult> BulkUpdateStockFromCSV(List<StockUpdateModel> csvData)
//        {
//            try
//            {
//                // Validate data
//                if (csvData == null || !csvData.Any())
//                {
//                    return new JsonResult(new { error = "No data provided" }) { StatusCode = 400 };
//                }

//                // Update stock using stored procedure
//                var success = await _spService.BulkUpdateStockAsync(csvData);

//                if (success)
//                {
//                    return new JsonResult(new 
//                    { 
//                        message = "Stock updated successfully", 
//                        updatedCount = csvData.Count 
//                    });
//                }
//                else
//                {
//                    return new JsonResult(new { error = "Failed to update stock" }) { StatusCode = 500 };
//                }
//            }
//            catch (Exception ex)
//            {
//                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
//            }
//        }

//        /// <summary>
//        /// Example 3: Process order with UPI payment
//        /// </summary>
//        public async Task<IActionResult> ProcessOrderWithUPI(int userId, decimal subtotal, decimal taxAmount, decimal totalAmount)
//        {
//            try
//            {
//                // Create order using stored procedure
//                var orderId = await _spService.CreateOrderAsync(
//                    userId: userId,
//                    subtotal: subtotal,
//                    taxAmount: taxAmount,
//                    totalAmount: totalAmount,
//                    paymentMethod: "upi",
//                    orderStatus: "Pending",
//                    notes: "UPI payment pending confirmation"
//                );

//                // Get order details
//                var orderDetails = await _spService.GetOrderDetailsAsync(orderId, userId);

//                return new JsonResult(new
//                {
//                    message = "Order created successfully",
//                    orderId = orderId,
//                    orderDetails = orderDetails,
//                    paymentInstructions = "Please complete UPI payment to confirm your order"
//                });
//            }
//            catch (Exception ex)
//            {
//                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
//            }
//        }

//        /// <summary>
//        /// Example 4: Get user's cart with product details
//        /// </summary>
//        public async Task<IActionResult> GetUserCart(int userId)
//        {
//            try
//            {
//                // Get cart items using stored procedure
//                var cartItems = await _spService.GetCartItemsAsync(userId);

//                // Calculate totals
//                var subtotal = cartItems.Sum(item => item.Product.Price * item.Quantity);
//                var taxAmount = subtotal * 0.1m; // 10% tax
//                var totalAmount = subtotal + taxAmount;

//                return new JsonResult(new
//                {
//                    CartItems = cartItems,
//                    Subtotal = subtotal,
//                    TaxAmount = taxAmount,
//                    TotalAmount = totalAmount,
//                    ItemCount = cartItems.Sum(item => item.Quantity)
//                });
//            }
//            catch (Exception ex)
//            {
//                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
//            }
//        }

//        /// <summary>
//        /// Example 5: Get products with advanced filtering
//        /// </summary>
//        public async Task<IActionResult> GetFilteredProducts(
//            string stockStatus = "all",
//            string? category = null,
//            string? searchTerm = null,
//            int page = 1,
//            int pageSize = 20)
//        {
//            try
//            {
//                // Get products using stored procedure with filtering
//                var products = await _spService.GetProductsByStockStatusAsync(
//                    stockStatus, category, searchTerm, page, pageSize);

//                return new JsonResult(new
//                {
//                    Products = products,
//                    Page = page,
//                    PageSize = pageSize,
//                    TotalCount = products.Count()
//                });
//            }
//            catch (Exception ex)
//            {
//                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
//            }
//        }

//        /// <summary>
//        /// Example 6: Auto-hide out of stock products
//        /// </summary>
//        public async Task<IActionResult> AutoHideOutOfStockProducts()
//        {
//            try
//            {
//                // Update product visibility using stored procedure
//                var success = await _spService.UpdateProductVisibilityAsync(autoHideOutOfStock: true);

//                if (success)
//                {
//                    return new JsonResult(new { message = "Out of stock products hidden successfully" });
//                }
//                else
//                {
//                    return new JsonResult(new { error = "Failed to update product visibility" }) { StatusCode = 500 };
//                }
//            }
//            catch (Exception ex)
//            {
//                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
//            }
//        }

//        /// <summary>
//        /// Example 7: Get sales analytics for reporting
//        /// </summary>
//        public async Task<IActionResult> GetSalesAnalytics(DateTime? startDate = null, DateTime? endDate = null)
//        {
//            try
//            {
//                // Get sales report using stored procedure
//                var salesReport = await _spService.GetSalesReportAsync(startDate, endDate);

//                return new JsonResult(new
//                {
//                    Report = salesReport,
//                    GeneratedAt = DateTime.Now,
//                    Period = new
//                    {
//                        StartDate = startDate ?? DateTime.Today.AddDays(-30),
//                        EndDate = endDate ?? DateTime.Today
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
//            }
//        }

//        /// <summary>
//        /// Example 8: Get user's order history with pagination
//        /// </summary>
//        public async Task<IActionResult> GetUserOrderHistory(int userId, int page = 1, int pageSize = 10)
//        {
//            try
//            {
//                // Get order history using stored procedure
//                var orders = await _spService.GetOrderHistoryAsync(userId, page, pageSize);

//                return new JsonResult(new
//                {
//                    Orders = orders,
//                    Page = page,
//                    PageSize = pageSize,
//                    UserId = userId
//                });
//            }
//            catch (Exception ex)
//            {
//                return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
//            }
//        }
//    }

//    /// <summary>
//    /// Example of how to integrate stored procedures into existing controllers
//    /// </summary>
//    public class EnhancedAdminController : ControllerBase
//    {
//        private readonly IStoredProcedureService _spService;

//        public EnhancedAdminController(IStoredProcedureService spService)
//        {
//            _spService = spService;
//        }

//        /// <summary>
//        /// Enhanced products view using stored procedures
//        /// </summary>
//        [HttpGet("products-enhanced")]
//        public async Task<IActionResult> GetProductsEnhanced(
//            [FromQuery] string stockStatus = "all",
//            [FromQuery] string? category = null,
//            [FromQuery] string? searchTerm = null,
//            [FromQuery] int page = 1,
//            [FromQuery] int pageSize = 20)
//        {
//            try
//            {
//                // Get products using stored procedure
//                var products = await _spService.GetProductsByStockStatusAsync(
//                    stockStatus, category, searchTerm, page, pageSize);

//                // Get stock summary for dashboard
//                var stockSummary = await _spService.GetStockAlertSummaryAsync();

//                return View(new
//                {
//                    Products = products,
//                    StockSummary = stockSummary,
//                    Filters = new
//                    {
//                        StockStatus = stockStatus,
//                        Category = category,
//                        SearchTerm = searchTerm,
//                        Page = page,
//                        PageSize = pageSize
//                    }
//                });
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = $"Error loading products: {ex.Message}";
//                return RedirectToAction("Products");
//            }
//        }

//        /// <summary>
//        /// Bulk stock update using stored procedures
//        /// </summary>
//        [HttpPost("bulk-update-stock")]
//        public async Task<IActionResult> BulkUpdateStock([FromBody] List<StockUpdateModel> stockUpdates)
//        {
//            try
//            {
//                if (stockUpdates == null || !stockUpdates.Any())
//                {
//                    TempData["Error"] = "No stock updates provided";
//                    return RedirectToAction("Products");
//                }

//                var success = await _spService.BulkUpdateStockAsync(stockUpdates);

//                if (success)
//                {
//                    TempData["Success"] = $"Successfully updated {stockUpdates.Count} products";
//                }
//                else
//                {
//                    TempData["Error"] = "Failed to update stock";
//                }

//                return RedirectToAction("Products");
//            }
//            catch (Exception ex)
//            {
//                TempData["Error"] = $"Error updating stock: {ex.Message}";
//                return RedirectToAction("Products");
//            }
//        }
//    }
//}
