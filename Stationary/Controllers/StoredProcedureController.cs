using Microsoft.AspNetCore.Mvc;
using Stationary.Models;
using Stationary.Services;

namespace Stationary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoredProcedureController : ControllerBase
    {
        private readonly IStoredProcedureService _spService;

        public StoredProcedureController(IStoredProcedureService spService)
        {
            _spService = spService;
        }

        /// <summary>
        /// Get stock alert summary
        /// </summary>
        [HttpGet("stock-alerts")]
        public async Task<IActionResult> GetStockAlerts()
        {
            try
            {
                var summary = await _spService.GetStockAlertSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving stock alerts", error = ex.Message });
            }
        }

        /// <summary>
        /// Get products by stock status with filtering and pagination
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string stockStatus = "all",
            [FromQuery] string? category = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var products = await _spService.GetProductsByStockStatusAsync(stockStatus, category, searchTerm, page, pageSize);
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving products", error = ex.Message });
            }
        }

        /// <summary>
        /// Bulk update stock quantities
        /// </summary>
        [HttpPost("bulk-update-stock")]
        public async Task<IActionResult> BulkUpdateStock([FromBody] List<StockUpdateModel> stockUpdates)
        {
            try
            {
                if (stockUpdates == null || !stockUpdates.Any())
                {
                    return BadRequest(new { message = "Stock updates are required" });
                }

                var success = await _spService.BulkUpdateStockAsync(stockUpdates);
                
                if (success)
                {
                    return Ok(new { message = "Stock updated successfully", updatedCount = stockUpdates.Count });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to update stock" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating stock", error = ex.Message });
            }
        }

        /// <summary>
        /// Get low stock alerts
        /// </summary>
        [HttpGet("low-stock-alerts")]
        public async Task<IActionResult> GetLowStockAlerts()
        {
            try
            {
                var alerts = await _spService.GetLowStockAlertsAsync();
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving low stock alerts", error = ex.Message });
            }
        }

        /// <summary>
        /// Update product visibility
        /// </summary>
        [HttpPost("update-visibility")]
        public async Task<IActionResult> UpdateVisibility([FromBody] UpdateVisibilityRequest request)
        {
            try
            {
                var success = await _spService.UpdateProductVisibilityAsync(request.AutoHideOutOfStock);
                
                if (success)
                {
                    return Ok(new { message = "Product visibility updated successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to update product visibility" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating visibility", error = ex.Message });
            }
        }

        /// <summary>
        /// Get cart items for a user
        /// </summary>
        [HttpGet("cart/{userId}")]
        public async Task<IActionResult> GetCartItems(int userId)
        {
            try
            {
                var cartItems = await _spService.GetCartItemsAsync(userId);
                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving cart items", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new order
        /// </summary>
        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Order request is required" });
                }

                var orderId = await _spService.CreateOrderAsync(
                    request.UserId,
                    request.Subtotal,
                    request.TaxAmount,
                    request.TotalAmount,
                    request.PaymentMethod ?? "cash",
                    request.OrderStatus ?? "Pending",
                    request.Notes
                );

                return Ok(new { message = "Order created successfully", orderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating order", error = ex.Message });
            }
        }

        /// <summary>
        /// Get order history for a user
        /// </summary>
        [HttpGet("orders/{userId}")]
        public async Task<IActionResult> GetOrderHistory(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var orders = await _spService.GetOrderHistoryAsync(userId, page, pageSize);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving order history", error = ex.Message });
            }
        }

        /// <summary>
        /// Get detailed order information
        /// </summary>
        [HttpGet("order/{orderId}/{userId}")]
        public async Task<IActionResult> GetOrderDetails(int orderId, int userId)
        {
            try
            {
                var order = await _spService.GetOrderDetailsAsync(orderId, userId);
                
                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving order details", error = ex.Message });
            }
        }

        /// <summary>
        /// Get sales report
        /// </summary>
        [HttpGet("sales-report")]
        public async Task<IActionResult> GetSalesReport([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var report = await _spService.GetSalesReportAsync(startDate, endDate);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving sales report", error = ex.Message });
            }
        }
    }

    // Request/Response Models
    public class UpdateVisibilityRequest
    {
        public bool AutoHideOutOfStock { get; set; } = true;
    }

    public class CreateOrderRequest
    {
        public int UserId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? OrderStatus { get; set; }
        public string? Notes { get; set; }
    }
}
