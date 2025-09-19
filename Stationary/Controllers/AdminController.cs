using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using Stationary.Data;
using Stationary.Models;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;
using Stationary.Services;
using Microsoft.Data.SqlClient;

namespace Stationary.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IProductService _productService;

        public AdminController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment, IProductService productService)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
            _productService = productService;
        }

        // GET: Login
        public IActionResult Login()
        {
            return View();
        }
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            return View(); // Create Product page
        }


        public async Task<IActionResult> Products(string search, string category, int page = 1, int pageSize = 20)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var products = _db.Products.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(search))
                    products = products.Where(p => p.Name.Contains(search) || p.Category.Contains(search));

                // Apply category filter
                if (!string.IsNullOrEmpty(category))
                    products = products.Where(p => p.Category == category);

                // Get total count for pagination
                var totalProducts = await products.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

                // Apply pagination
                var pagedProducts = await products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get unique categories for filter dropdown
                var categories = await _db.Products
                    .Select(p => p.Category)
                    .Distinct()
                    .ToListAsync();

                // Check SP/TVP availability for banner
                ViewBag.InventorySpOk = CheckInventorySpAvailability(out string spInfo);
                ViewBag.InventorySpMsg = spInfo;

                ViewBag.Search = search;
                ViewBag.Category = category;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.Categories = categories;
                ViewBag.PageSize = pageSize;

                return View(pagedProducts);
            }
            catch (Exception ex)
            {
                // Log the exception (in production, use proper logging)
                TempData["Error"] = "An error occurred while loading products.";
                return View(new List<Product>());
            }
        }

        // Stock Management
        public async Task<IActionResult> StockManagement()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var outOfStockProducts = await _productService.GetOutOfStockProductsAsync();
                var lowStockProducts = await _productService.GetLowStockProductsAsync();
                var allProducts = await _productService.GetAvailableProductsAsync(true);

                ViewBag.OutOfStockCount = outOfStockProducts.Count();
                ViewBag.LowStockCount = lowStockProducts.Count();
                ViewBag.TotalProducts = allProducts.Count();

                return View(new StockManagementViewModel
                {
                    OutOfStockProducts = outOfStockProducts.ToList(),
                    LowStockProducts = lowStockProducts.ToList(),
                    AllProducts = allProducts.ToList()
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading stock information.";
                return View(new StockManagementViewModel());
            }
        }

        // Stock Alerts Dashboard
        public async Task<IActionResult> StockAlerts()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var stockSummary = await _productService.GetStockAlertSummaryAsync();
                var outOfStockProducts = await _productService.GetOutOfStockProductsAsync();
                var lowStockProducts = await _productService.GetLowStockProductsAsync();

                ViewBag.StockSummary = stockSummary;
                ViewBag.OutOfStockProducts = outOfStockProducts.ToList();
                ViewBag.LowStockProducts = lowStockProducts.ToList();

                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading stock alerts.";
                return View();
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkUpdateStock(List<StockUpdateModel> stockUpdates)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                foreach (var update in stockUpdates)
                {
                    var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == update.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity = update.NewStockQuantity;
                        product.LowStockThreshold = update.NewLowStockThreshold;
                    }
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = "Stock updated successfully for all products!";
                return RedirectToAction("StockManagement");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating stock.";
                return RedirectToAction("StockManagement");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product, IFormFile image)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(product.Name) || product.Price <= 0 || product.StockQuantity < 0)
                {
                    TempData["Error"] = "Please provide valid product information.";
                    return View(product);
                }

                if (image != null && image.Length > 0)
                {
                    // Validate image
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Please upload a valid image file (JPG, PNG, GIF).";
                        return View(product);
                    }

                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid().ToString() + fileExtension; // Use GUID to prevent conflicts
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    product.ImagePath = "/images/" + fileName;
                }

                _db.Products.Add(product);
                await _db.SaveChangesAsync();

                TempData["Success"] = "Product created successfully!";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while creating the product.";
                return View(product);
            }
        }

        // Edit product (GET)
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Products");
                }

                return View(product);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading the product.";
                return RedirectToAction("Products");
            }
        }

        // Edit product (POST)
        [HttpPost]
        public async Task<IActionResult> Edit(Product model, IFormFile image)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == model.Id);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Products");
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(model.Name) || model.Price <= 0 || model.StockQuantity < 0)
                {
                    TempData["Error"] = "Please provide valid product information.";
                    return View(model);
                }

                product.Name = model.Name;
                product.Category = model.Category;
                product.Price = model.Price;
                product.StockQuantity = model.StockQuantity;
                product.LowStockThreshold = model.LowStockThreshold;
                product.IsVisible = model.IsVisible;

                if (image != null && image.Length > 0)
                {
                    // Validate image
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                    
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["Error"] = "Please upload a valid image file (JPG, PNG, GIF).";
                        return View(model);
                    }

                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string fileName = Guid.NewGuid().ToString() + fileExtension; // Use GUID to prevent conflicts
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }
                    product.ImagePath = "/images/" + fileName;
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating the product.";
                return View(model);
            }
        }

        // Delete product (GET via link)
        [HttpGet]
        public IActionResult Delete(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var product = _db.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
                return RedirectToAction("Products");

            // Remove any cart items referencing this product
            var carts = _db.Carts.Where(c => c.ProductId == id).ToList();
            if (carts.Any())
                _db.Carts.RemoveRange(carts);

            // Block delete if product has order history
            bool usedInOrders = _db.Orders.Any(o => o.OrderItems.Any(oi => oi.ProductId == id));
            if (usedInOrders)
            {
                TempData["Error"] = "Cannot delete product because it exists in past orders.";
                return RedirectToAction("Products");
            }

            _db.Products.Remove(product);
            _db.SaveChanges();
            TempData["Success"] = "Product deleted successfully.";
            return RedirectToAction("Products");
        }

        // Toggle product visibility
        [HttpPost]
        public async Task<IActionResult> ToggleVisibility(int id)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return Json(new { success = false, message = "Unauthorized" });

            try
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
                if (product == null)
                    return Json(new { success = false, message = "Product not found" });

                product.IsVisible = !product.IsVisible;
                await _db.SaveChangesAsync();

                return Json(new { success = true, isVisible = product.IsVisible, message = $"Product {(product.IsVisible ? "shown" : "hidden")} successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating visibility" });
            }
        }

        // Reports
        [HttpGet]
        public IActionResult Reports(DateTime? date)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            DateTime selected = (date ?? DateTime.Today).Date;
            var vm = TryBuildReportWithStoredProcedure(selected) ?? BuildReportWithEf(selected);
            return View(vm);
        }

        [HttpGet]
        public IActionResult ReportsExcel(DateTime? date)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            DateTime selected = (date ?? DateTime.Today).Date;
            var vm = TryBuildReportWithStoredProcedure(selected) ?? BuildReportWithEf(selected);

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Sales Report");
                int row = 1;
                sheet.Cell(row, 1).Value = $"Sales Report - {selected:yyyy-MM-dd}";
                sheet.Range(row, 1, row, 5).Merge().Style.Font.SetBold().Font.FontSize = 14;
                row += 2;

                // Headers
                sheet.Cell(row, 1).Value = "Order ID";
                sheet.Cell(row, 2).Value = "Date";
                sheet.Cell(row, 3).Value = "User";
                sheet.Cell(row, 4).Value = "Items";
                sheet.Cell(row, 5).Value = "Amount";
                sheet.Range(row, 1, row, 5).Style.Font.SetBold();
                row++;

                foreach (var r in vm.Rows.OrderBy(r => r.OrderDate))
                {
                    sheet.Cell(row, 1).Value = r.OrderId;
                    sheet.Cell(row, 2).Value = r.OrderDate.ToString("yyyy-MM-dd HH:mm");
                    sheet.Cell(row, 3).Value = r.Username;
                    sheet.Cell(row, 4).Value = r.Items;
                    sheet.Cell(row, 5).Value = r.Amount;
                    row++;
                }

                row++;
                sheet.Cell(row, 4).Value = "Total Orders:";
                sheet.Cell(row, 5).Value = vm.TotalOrders;
                row++;
                sheet.Cell(row, 4).Value = "Total Items:";
                sheet.Cell(row, 5).Value = vm.TotalItemsSold;
                row++;
                sheet.Cell(row, 4).Value = "Total Sales:";
                sheet.Cell(row, 5).Value = vm.TotalSalesAmount;

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    var fileName = $"SalesReport_{selected:yyyyMMdd}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        [HttpGet]
        public IActionResult ReportsPdf(DateTime? date)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            DateTime selected = (date ?? DateTime.Today).Date;
            var vm = TryBuildReportWithStoredProcedure(selected) ?? BuildReportWithEf(selected);

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text($"Sales Report - {selected:yyyy-MM-dd}").SemiBold().FontSize(18);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Order ID").SemiBold();
                            header.Cell().Element(CellStyle).Text("Date").SemiBold();
                            header.Cell().Element(CellStyle).Text("User").SemiBold();
                            header.Cell().Element(CellStyle).Text("Items").SemiBold();
                            header.Cell().Element(CellStyle).Text("Amount").SemiBold();

                            static IContainer CellStyle(IContainer container) => container.PaddingVertical(4).DefaultTextStyle(x => x.FontSize(10));
                        });

                        foreach (var r in vm.Rows)
                        {
                            table.Cell().Element(Cell).Text(r.OrderId.ToString());
                            table.Cell().Element(Cell).Text(r.OrderDate.ToString("yyyy-MM-dd HH:mm"));
                            table.Cell().Element(Cell).Text(r.Username);
                            table.Cell().Element(Cell).Text(r.Items.ToString());
                            table.Cell().Element(Cell).Text(r.Amount.ToString("0.00"));

                            static IContainer Cell(IContainer container) => container.PaddingVertical(3).DefaultTextStyle(x => x.FontSize(10));
                        }
                    });

                    page.Footer().AlignRight().Text($"Totals — Orders: {vm.TotalOrders} | Items: {vm.TotalItemsSold} | Sales: {vm.TotalSalesAmount:0.00}").FontSize(10);
                });
            }).GeneratePdf();

            var pdfName = $"SalesReport_{selected:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", pdfName);
        }

        private SalesReportViewModel? TryBuildReportWithStoredProcedure(DateTime selected)
        {
            try
            {
                var rows = new List<SalesReportRow>();
                int totalOrders = 0;
                int totalItems = 0;
                decimal totalSales = 0m;

                var connection = _db.Database.GetDbConnection();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.usp_GetDailySalesReport";
                    command.CommandType = CommandType.StoredProcedure;

                    var p = command.CreateParameter();
                    p.ParameterName = "@ReportDate";
                    p.Value = selected.Date;
                    command.Parameters.Add(p);

                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        // Result set 1: rows
                        while (reader.Read())
                        {
                            rows.Add(new SalesReportRow
                            {
                                OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                                OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                                Username = reader.GetString(reader.GetOrdinal("Username")),
                                Items = reader.GetInt32(reader.GetOrdinal("Items")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount"))
                            });
                        }
                        // Result set 2: totals
                        if (reader.NextResult() && reader.Read())
                        {
                            totalOrders = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                            totalItems = reader.GetInt32(reader.GetOrdinal("TotalItemsSold"));
                            totalSales = reader.GetDecimal(reader.GetOrdinal("TotalSalesAmount"));
                        }
                    }
                }

                return new SalesReportViewModel
                {
                    SelectedDate = selected,
                    Rows = rows.OrderByDescending(r => r.OrderDate).ToList(),
                    TotalOrders = totalOrders,
                    TotalItemsSold = totalItems,
                    TotalSalesAmount = totalSales
                };
            }
            catch
            {
                return null; // fall back to EF
            }
        }

        private SalesReportViewModel BuildReportWithEf(DateTime selected)
        {
            DateTime start = selected;
            DateTime end = selected.AddDays(1);

            var orders = _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Date >= start && o.Date < end)
                .ToList();

            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var users = _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Username);

            return new SalesReportViewModel
            {
                SelectedDate = selected,
                TotalOrders = orders.Count,
                TotalSalesAmount = orders.Sum(o => o.TotalAmount),
                TotalItemsSold = orders.Sum(o => o.OrderItems != null ? o.OrderItems.Sum(i => i.Quantity) : 0),
                Rows = orders.OrderByDescending(o => o.Date).Select(o => new SalesReportRow
                {
                    OrderId = o.Id,
                    OrderDate = o.Date,
                    Username = users.TryGetValue(o.UserId, out var name) ? name : "User#" + o.UserId,
                    Items = o.OrderItems != null ? o.OrderItems.Sum(i => i.Quantity) : 0,
                    Amount = o.TotalAmount
                }).ToList()
            };
        }

        [HttpGet]
        public IActionResult InventoryUpload()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            ViewBag.InventorySpOk = CheckInventorySpAvailability(out string spInfo);
            ViewBag.InventorySpMsg = spInfo;

            return View(Enumerable.Empty<OcrInventoryItem>());
        }

        // Bulk Product Creation - GET
        [HttpGet]
        public IActionResult BulkCreate()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            return View();
        }

        // Bulk Product Creation - POST (Form-based)
        [HttpPost]
        public async Task<IActionResult> BulkCreate(List<BulkProductModel> products)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                if (products == null || !products.Any())
                {
                    TempData["Error"] = "No products provided.";
                    return View(products);
                }

                var createdCount = 0;
                var errors = new List<string>();

                foreach (var productModel in products)
                {
                    if (string.IsNullOrWhiteSpace(productModel.Name) || productModel.Price <= 0)
                    {
                        errors.Add($"Invalid product: {productModel.Name ?? "Unknown"}");
                        continue;
                    }

                    var product = new Product
                    {
                        Name = productModel.Name.Trim(),
                        Category = productModel.Category?.Trim() ?? "Uncategorized",
                        Price = productModel.Price,
                        StockQuantity = productModel.StockQuantity,
                        LowStockThreshold = productModel.LowStockThreshold,
                        IsVisible = productModel.IsVisible
                    };

                    _db.Products.Add(product);
                    createdCount++;
                }

                await _db.SaveChangesAsync();
                TempData["Success"] = $"Successfully created {createdCount} products!";
                
                if (errors.Any())
                {
                    TempData["Warning"] = $"Some products had errors: {string.Join(", ", errors)}";
                }

                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while creating products.";
                return View(products);
            }
        }

        // CSV Upload for Bulk Products
        [HttpPost]
        public async Task<IActionResult> BulkCreateFromCsv(IFormFile csvFile)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            try
            {
                if (csvFile == null || csvFile.Length == 0)
                {
                    TempData["Error"] = "Please select a CSV file.";
                    return RedirectToAction("BulkCreate");
                }

                var products = new List<Product>();
                var errors = new List<string>();
                var lineNumber = 0;

                using (var reader = new StreamReader(csvFile.OpenReadStream()))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        lineNumber++;
                        if (lineNumber == 1) continue; // Skip header

                        var values = line.Split(',');
                        if (values.Length < 4) continue;

                        try
                        {
                            var product = new Product
                            {
                                Name = values[0]?.Trim() ?? "",
                                Category = values[1]?.Trim() ?? "Uncategorized",
                                Price = decimal.Parse(values[2]?.Trim() ?? "0"),
                                StockQuantity = int.Parse(values[3]?.Trim() ?? "0"),
                                LowStockThreshold = values.Length > 4 ? int.Parse(values[4]?.Trim() ?? "5") : 5,
                                IsVisible = values.Length > 5 ? bool.Parse(values[5]?.Trim() ?? "true") : true
                            };

                            if (!string.IsNullOrWhiteSpace(product.Name) && product.Price > 0)
                            {
                                products.Add(product);
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Line {lineNumber}: {ex.Message}");
                        }
                    }
                }

                if (products.Any())
                {
                    _db.Products.AddRange(products);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = $"Successfully created {products.Count} products from CSV!";
                }

                if (errors.Any())
                {
                    TempData["Warning"] = $"Some lines had errors: {string.Join(", ", errors.Take(5))}";
                }

                return RedirectToAction("Products");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing the CSV file.";
                return RedirectToAction("BulkCreate");
            }
        }

        // Download CSV Template
        [HttpGet]
        public IActionResult DownloadCsvTemplate()
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var csv = "Name,Category,Price,StockQuantity,LowStockThreshold,IsVisible\n" +
                     "Sample Product 1,Office Supplies,9.99,50,10,true\n" +
                     "Sample Product 2,Electronics,29.99,25,5,true\n" +
                     "Sample Product 3,Books,15.50,100,20,true";

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "product_template.csv");
        }

        [HttpPost]
        public IActionResult InventoryUpload(IFormFile file, int? defaultThreshold)
        {
            if (HttpContext.Session.GetString("Role") != "Admin")
                return RedirectToAction("Login", "Account");

            var service = new OcrInventoryService();
            string? tessPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            var items = service.ExtractItems(file, tessPath, out string message);

            int created = 0, updated = 0;
            int threshold = defaultThreshold.HasValue ? Math.Max(0, defaultThreshold.Value) : 5;

            if (items != null && items.Any())
            {
                // Try TVP stored procedure first
                if (!TryUpsertInventoryWithStoredProcedure(items, threshold, ref created, ref updated, out string spMessage))
                {
                    // Fallback to EF
                    foreach (var it in items)
                    {
                        var existing = _db.Products.FirstOrDefault(p => p.Name.ToLower() == it.ProductName.ToLower());
                        if (existing == null)
                        {
                            _db.Products.Add(new Product
                            {
                                Name = it.ProductName,
                                Category = "Uncategorized",
                                Price = 0,
                                ImagePath = string.Empty,
                                StockQuantity = it.Quantity,
                                LowStockThreshold = threshold
                            });
                            created++;
                        }
                        else
                        {
                            existing.StockQuantity += it.Quantity;
                            if (existing.LowStockThreshold <= 0)
                                existing.LowStockThreshold = threshold;
                            updated++;
                        }
                    }
                    _db.SaveChanges();
                    if (!string.IsNullOrEmpty(spMessage))
                        message = string.IsNullOrEmpty(message) ? spMessage : message + " | " + spMessage;
                }
            }

            TempData["ImportMessage"] = string.IsNullOrEmpty(message)
                ? $"Processed: {items.Count} lines. Created {created}, Updated {updated}."
                : message + (items != null ? $" | Parsed {items.Count} lines." : string.Empty);

            ViewBag.InventorySpOk = CheckInventorySpAvailability(out string spInfo2);
            ViewBag.InventorySpMsg = spInfo2;

            return View(items ?? Enumerable.Empty<OcrInventoryItem>());
        }

        private bool TryUpsertInventoryWithStoredProcedure(List<OcrInventoryItem> items, int threshold, ref int created, ref int updated, out string info)
        {
            info = string.Empty;
            try
            {
                using var conn = new SqlConnection(_db.Database.GetConnectionString());
                using var cmd = new SqlCommand("dbo.usp_UpsertInventoryItems", conn) { CommandType = CommandType.StoredProcedure };

                var tvp = new DataTable();
                tvp.Columns.Add("ProductName", typeof(string));
                tvp.Columns.Add("Quantity", typeof(int));
                tvp.Columns.Add("LowStockThreshold", typeof(int));

                foreach (var it in items)
                {
                    tvp.Rows.Add(it.ProductName, it.Quantity, threshold);
                }

                var param = cmd.Parameters.AddWithValue("@Items", tvp);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = "dbo.InventoryItemTv";

                conn.Open();
                cmd.ExecuteNonQuery();

                // Post-estimate: created vs updated requires a lookup. Keep simple: set info text.
                info = "Stored procedure executed.";
                return true;
            }
            catch (Exception ex)
            {
                info = "SP fallback: " + ex.Message;
                return false;
            }
        }

        private bool CheckInventorySpAvailability(out string info)
        {
            info = string.Empty;
            try
            {
                using var conn = new SqlConnection(_db.Database.GetConnectionString());
                using var cmd = new SqlCommand(@"SELECT
    CONVERT(int, CASE WHEN EXISTS (SELECT 1 FROM sys.types WHERE name='InventoryItemTv' AND schema_id = SCHEMA_ID('dbo')) THEN 1 ELSE 0 END) AS HasType,
    CONVERT(int, CASE WHEN EXISTS (SELECT 1 FROM sys.procedures WHERE name='usp_UpsertInventoryItems' AND schema_id = SCHEMA_ID('dbo')) THEN 1 ELSE 0 END) AS HasProc;", conn);
                conn.Open();
                using var reader = cmd.ExecuteReader();
                int hasType = 0, hasProc = 0;
                if (reader.Read())
                {
                    hasType = reader.GetInt32(0);
                    hasProc = reader.GetInt32(1);
                }
                bool ok = (hasType == 1 && hasProc == 1);
                info = ok ? "Inventory import SP/TVP available." : "Inventory import SP/TVP missing. Please run provided SQL scripts.";
                return ok;
            }
            catch (Exception ex)
            {
                info = "Inventory SP check failed: " + ex.Message;
                return false;
            }
        }
    }
}
