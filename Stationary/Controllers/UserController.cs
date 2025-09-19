using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;
using Stationary.Services;

namespace Stationary.Controllers
{
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IProductService _productService;
        private readonly ICartService _cartService;

        public UserController(ApplicationDbContext db, IProductService productService, ICartService cartService)
        {
            _db = db;
            _productService = productService;
            _cartService = cartService;
        }

        public IActionResult Login()
        {
            return View();
        }



        // Product List with Search
        public async Task<IActionResult> Index(string search, string category, string stockFilter = "available", int page = 1, int pageSize = 12)
        {
            if (HttpContext.Session.GetString("Role") != "User")
                return RedirectToAction("Login", "User");

            IEnumerable<Product> products;

            // Apply stock filter
            switch (stockFilter?.ToLower())
            {
                case "all":
                    products = await _productService.GetAvailableProductsAsync(true);
                    break;
                case "outofstock":
                    products = await _productService.GetOutOfStockProductsAsync();
                    break;
                case "lowstock":
                    products = await _productService.GetLowStockProductsAsync();
                    break;
                default:
                    products = await _productService.GetAvailableProductsAsync(false);
                    break;
            }

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
                products = products.Where(p =>
                    p.Name != null && p.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

            // Apply category filter
            if (!string.IsNullOrEmpty(category))
                products = products.Where(p =>
                    p.Category != null && p.Category.Contains(category, StringComparison.OrdinalIgnoreCase));

            // Get total count for pagination
            var totalProducts = products.Count();
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // Apply pagination
            var pagedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Get unique categories for filter dropdown
            var categories = await _productService.GetCategoriesAsync();

            ViewBag.Search = search;
            ViewBag.Category = category;
            ViewBag.StockFilter = stockFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Categories = categories;
            ViewBag.PageSize = pageSize;

            return View(pagedProducts);
        }


        [HttpPost]
        public async Task<JsonResult> AddToCart(int id, int quantity = 1)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return Json(new { success = false, message = "Please login first.", redirect = true });

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return Json(new { success = false, message = "User not found.", redirect = true });

                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                    return Json(new { success = false, message = "Product not found." });

                // Validate quantity
                if (quantity <= 0)
                    return Json(new { success = false, message = "Quantity must be greater than 0." });

                // Check if product is out of stock
                if (product.IsOutOfStock)
                    return Json(new { success = false, message = "This product is currently out of stock." });

                // Check stock availability - consider existing cart items
                var existingCartItem = await _db.Carts.FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);
                var currentCartQuantity = existingCartItem?.Quantity ?? 0;
                var availableStock = product.StockQuantity - currentCartQuantity;

                if (quantity > availableStock)
                    return Json(new { success = false, message = $"Only {availableStock} more items available in stock. (You already have {currentCartQuantity} in your cart)" });

                if (existingCartItem == null)
                {
                    // New product → add with selected quantity
                    _db.Carts.Add(new Cart { UserId = user.Id, ProductId = id, Quantity = quantity });
                }
                else
                {
                    // Product already in cart → update quantity
                    existingCartItem.Quantity = quantity;
                }

                await _db.SaveChangesAsync();

                // Count total quantity across all items
                var cartCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);

                return Json(new { success = true, count = cartCount, message = "Product added to cart successfully!" });
            }
            catch (Exception ex)
            {
                // Log the exception (in production, use proper logging)
                return Json(new { success = false, message = "An error occurred while adding to cart." });
            }
        }


        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return RedirectToAction("Login", "User");

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return RedirectToAction("Login", "User");

                var cart = await _db.Carts
                    .Include(c => c.Product)
                    .Where(c => c.UserId == user.Id)
                    .ToListAsync();

                // Calculate total price
                var totalPrice = cart.Sum(c => c.Product.Price * c.Quantity);
                ViewBag.TotalPrice = totalPrice;
                ViewBag.ItemCount = cart.Count;

                return View(cart);
            }
            catch (Exception ex)
            {
                // Log the exception (in production, use proper logging)
                return RedirectToAction("Error", "Home");
            }
        }


        [HttpPost]
        public async Task<JsonResult> RemoveFromCart(int id)
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return Json(new { success = false, message = "Please login first." });

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return Json(new { success = false, message = "User not found." });

                var cartItem = await _db.Carts.FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);
                if (cartItem != null)
                {
                    _db.Carts.Remove(cartItem);
                    await _db.SaveChangesAsync();
                }

                // Get updated cart count
                var cartCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);

                return Json(new { success = true, count = cartCount, message = "Item removed from cart successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while removing item from cart." });
            }
        }

        [HttpPost]
        public async Task<JsonResult> UpdateCartQuantity(int id, int quantity)
        {
            var username = HttpContext.Session.GetString("Username");
            if (string.IsNullOrEmpty(username))
                return Json(new { success = false, message = "Please login first.", redirect = true });

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Json(new { success = false, message = "User not found.", redirect = true });

            var cartItem = await _db.Carts.FirstOrDefaultAsync(c => c.ProductId == id && c.UserId == user.Id);
            if (cartItem == null)
                return Json(new { success = false, message = "Cart item not found." });

            // Validate quantity
            if (quantity <= 0)
            {
                // Remove item from cart if quantity is 0 or negative
                _db.Carts.Remove(cartItem);
                await _db.SaveChangesAsync();
                var cartCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);
                return Json(new { success = true, count = cartCount, message = "Item removed from cart." });
            }

            // Check stock availability
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null)
                return Json(new { success = false, message = "Product not found." });

            if (product.StockQuantity < quantity)
                return Json(new { success = false, message = $"Only {product.StockQuantity} items available in stock." });

            // Update quantity
            cartItem.Quantity = quantity;
            await _db.SaveChangesAsync();

            // return updated cart count
            var updatedCartCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);

            return Json(new { success = true, count = updatedCartCount });
        }

        [HttpGet]
        public async Task<JsonResult> GetCartCount()
        {
            try
            {
                var username = HttpContext.Session.GetString("Username");
                if (string.IsNullOrEmpty(username))
                    return Json(new { count = 0 });

                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
                if (user == null)
                    return Json(new { count = 0 });

                var cartCount = await _db.Carts.Where(c => c.UserId == user.Id).SumAsync(c => c.Quantity);
                return Json(new { count = cartCount });
            }
            catch (Exception ex)
            {
                return Json(new { count = 0 });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Checkout(string paymentMethod = "cash")
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId == null)
                    return RedirectToAction("Login", "User");

                 var user = await _db.Users.FindAsync(userId);
                if (user == null)
                    return RedirectToAction("Login", "User");

                var cart = await _db.Carts
                                    .Include(c => c.Product)
                                    .Where(c => c.UserId == user.Id)
                                    .ToListAsync();

                if (!cart.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Cart");
                }

                // validate stock
                foreach (var item in cart)
                {
                    if (item.Product == null || item.Product.StockQuantity < item.Quantity)
                    {
                        TempData["Error"] = $"Insufficient stock for {item.Product?.Name ?? "Unknown Product"}.";
                        return RedirectToAction("Cart");
                    }
                }

                // deduct stock
                foreach (var item in cart)
                {
                    item.Product.StockQuantity -= item.Quantity;
                    _db.Products.Update(item.Product);
                }

                var subtotal = cart.Sum(c => c.Product.Price * c.Quantity);
                var tax = subtotal * 0.1m; // 10% tax
                var total = subtotal + tax;

                var order = new Order
                {
                    UserId = user.Id,
                    TotalAmount = total,
                    Date = DateTime.Now,
                    PaymentMethod = paymentMethod,
                    OrderItems = cart.Select(c => new OrderItem
                    {
                        ProductId = c.ProductId,
                        Quantity = c.Quantity,
                        ProductName = c.Product.Name,
                        Price = c.Product.Price
                    }).ToList()
                };

                _db.Orders.Add(order);
                _db.Carts.RemoveRange(cart);

                await _db.SaveChangesAsync();

                var successMessage = paymentMethod == "upi" 
                    ? "Order placed successfully! Please complete the UPI payment to confirm your order." 
                    : "Order placed successfully! You can pay cash on delivery.";
                    
                TempData["Success"] = successMessage;
                return RedirectToAction("Index", "User");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Checkout failed: {ex.Message}";
                return RedirectToAction("Cart");
            }
        }

    }

}



    
