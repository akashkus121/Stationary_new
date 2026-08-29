using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using Stationary.Models;
using Stationary.Services;
using System.Text;

namespace Stationary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/adminapi")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IProductService _productService;
        private readonly IEventStreamService _eventStream;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IOfflineFallbackQueueService _fallbackQueue;
        private readonly IRedisCacheService _redisCache;

        public AdminController(
            ApplicationDbContext db,
            IProductService productService,
            IEventStreamService eventStream,
            ICloudinaryService cloudinaryService,
            IOfflineFallbackQueueService fallbackQueue,
            IRedisCacheService redisCache)
        {
            _db = db;
            _productService = productService;
            _eventStream = eventStream;
            _cloudinaryService = cloudinaryService;
            _fallbackQueue = fallbackQueue;
            _redisCache = redisCache;
        }

        private async Task<User?> GetCurrentAuthUserAsync()
        {
            int? userId = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst("id")?.Value
                           ?? User.FindFirst("sub")?.Value;
                if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out var parsedId))
                {
                    userId = parsedId;
                }
            }

            if (userId == null && Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var headerStr = authHeader.ToString();
                if (headerStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var tokenStr = headerStr.Substring("Bearer ".Length).Trim();
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        if (handler.CanReadToken(tokenStr))
                        {
                            var jwt = handler.ReadJwtToken(tokenStr);
                            var claim = jwt.Claims.FirstOrDefault(c =>
                                c.Type == System.Security.Claims.ClaimTypes.NameIdentifier ||
                                c.Type == "id" ||
                                c.Type == "nameid" ||
                                c.Type == "sub")?.Value;
                            if (int.TryParse(claim, out var jwtId))
                            {
                                userId = jwtId;
                            }
                        }
                    }
                    catch { }
                }
            }

            if (userId == null)
            {
                try
                {
                    userId = HttpContext.Session.GetInt32("UserId");
                }
                catch { }
            }

            if (userId == null) return null;

            return await _db.Users.FindAsync(userId);
        }

        [HttpGet("stock-management")]
        public async Task<IActionResult> GetStockManagement()
        {
            try
            {
                var outOfStockProducts = (await _productService.GetOutOfStockProductsAsync()).ToList();
                var lowStockProducts = (await _productService.GetLowStockProductsAsync()).ToList();
                var allProducts = (await _productService.GetAvailableProductsAsync(true)).ToList();

                return Ok(new
                {
                    outOfStockCount = outOfStockProducts.Count,
                    lowStockCount = lowStockProducts.Count,
                    totalProducts = allProducts.Count,
                    outOfStockProducts,
                    lowStockProducts,
                    allProducts
                });
            }
            catch (Exception ex)
            {
                // Fallback to offline cached products if SQL is unreachable
                var cached = await _fallbackQueue.GetProductCacheAsync();
                return Ok(new
                {
                    isFallback = true,
                    fallbackMessage = "SQL Database unreachable. Displaying cached inventory fallback data.",
                    outOfStockCount = cached.Count(p => p.StockQuantity <= 0),
                    lowStockCount = cached.Count(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold),
                    totalProducts = cached.Count,
                    outOfStockProducts = cached.Where(p => p.StockQuantity <= 0).ToList(),
                    lowStockProducts = cached.Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold).ToList(),
                    allProducts = cached
                });
            }
        }

        [HttpGet("stock-alerts")]
        public async Task<IActionResult> GetStockAlerts()
        {
            try
            {
                var stockSummary = await _productService.GetStockAlertSummaryAsync();
                var outOfStockProducts = (await _productService.GetOutOfStockProductsAsync()).ToList();
                var lowStockProducts = (await _productService.GetLowStockProductsAsync()).ToList();

                return Ok(new
                {
                    stockSummary,
                    outOfStockProducts,
                    lowStockProducts
                });
            }
            catch (Exception)
            {
                var cached = await _fallbackQueue.GetProductCacheAsync();
                return Ok(new
                {
                    isFallback = true,
                    stockSummary = new StockAlertSummary
                    {
                        TotalProducts = cached.Count,
                        OutOfStockProducts = cached.Count(p => p.StockQuantity <= 0),
                        LowStockProducts = cached.Count(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold),
                        InStockProducts = cached.Count(p => p.StockQuantity > p.LowStockThreshold)
                    },
                    outOfStockProducts = cached.Where(p => p.StockQuantity <= 0).ToList(),
                    lowStockProducts = cached.Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold).ToList()
                });
            }
        }

        [HttpPost("bulk-update-stock")]
        public async Task<IActionResult> BulkUpdateStock([FromBody] List<StockUpdateModel> stockUpdates)
        {
            if (stockUpdates == null || !stockUpdates.Any())
            {
                return BadRequest(new { message = "No stock updates provided." });
            }

            try
            {
                foreach (var update in stockUpdates)
                {
                    var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == update.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity = Math.Max(0, update.NewStockQuantity);
                        product.LowStockThreshold = Math.Max(0, update.NewLowStockThreshold);
                    }
                }

                await _db.SaveChangesAsync();

                // Non-blocking background sync
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _redisCache.RemoveByPatternAsync("products:*");
                        var all = await _db.Products.AsNoTracking().ToListAsync();
                        await _fallbackQueue.SaveProductCacheAsync(all);
                    }
                    catch { }
                });

                _eventStream.BroadcastEvent("stock_update", new
                {
                    action = "bulk_stock_update",
                    count = stockUpdates.Count,
                    timestamp = DateTime.UtcNow
                });

                return Ok(new { success = true, message = "Stock updated successfully for all specified products!" });
            }
            catch (Exception ex)
            {
                // Enqueue write operation to resilient fallback queue
                await _fallbackQueue.EnqueuePendingActionAsync("bulk_stock_update", stockUpdates);

                return Ok(new
                {
                    success = true,
                    isQueued = true,
                    message = "SQL database unreachable. Stock updates have been queued to resilient fallback storage and will auto-sync when SQL comes online!"
                });
            }
        }

        [HttpPost("bulk-create")]
        public async Task<IActionResult> BulkCreate([FromBody] List<BulkProductModel> products)
        {
            if (products == null || !products.Any())
            {
                return BadRequest(new { message = "No products provided." });
            }

            var currentAdmin = await GetCurrentAuthUserAsync();
            var createdCount = 0;
            var errors = new List<string>();
            var entities = new List<Product>();

            foreach (var p in products)
            {
                if (string.IsNullOrWhiteSpace(p.Name) || p.Price <= 0)
                {
                    errors.Add($"Invalid product: {p.Name ?? "Unknown"}");
                    continue;
                }

                var imgUrl = _cloudinaryService.ProcessImageUrl(p.ImageUrl ?? p.ImagePath, p.Category, p.Name);

                var product = new Product
                {
                    Name = p.Name.Trim(),
                    Category = ProductService.NormalizeCategory(p.Category),
                    Price = p.Price,
                    StockQuantity = Math.Max(0, p.StockQuantity),
                    LowStockThreshold = p.LowStockThreshold <= 0 ? 5 : p.LowStockThreshold,
                    IsVisible = p.IsVisible,
                    ImagePath = imgUrl,
                    AdminId = currentAdmin?.Id,
                    AdminUsername = currentAdmin?.Username
                };

                entities.Add(product);
                createdCount++;
            }

            try
            {
                await _db.Products.AddRangeAsync(entities);
                await _db.SaveChangesAsync();

                // Fire non-blocking cache invalidation & offline sync in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _redisCache.RemoveByPatternAsync("products:*");
                        var all = await _db.Products.AsNoTracking().ToListAsync();
                        await _fallbackQueue.SaveProductCacheAsync(all);
                    }
                    catch { }
                });

                _eventStream.BroadcastEvent("stock_update", new
                {
                    action = "bulk_create",
                    createdCount,
                    timestamp = DateTime.UtcNow
                });

                return Ok(new
                {
                    success = true,
                    createdCount,
                    errors,
                    message = $"Successfully created {createdCount} products."
                });
            }
            catch (Exception)
            {
                await _fallbackQueue.EnqueuePendingActionAsync("bulk_create", products);

                return Ok(new
                {
                    success = true,
                    isQueued = true,
                    createdCount,
                    errors,
                    message = $"SQL Server unreachable. {createdCount} products enqueued to resilient fallback queue for auto-sync!"
                });
            }
        }

        [HttpPost("bulk-create-csv")]
        public async Task<IActionResult> BulkCreateFromCsv(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                return BadRequest(new { message = "Please select a CSV file." });
            }

            var currentAdmin = await GetCurrentAuthUserAsync();
            var products = new List<Product>();
            var bulkModels = new List<BulkProductModel>();
            var errors = new List<string>();
            var lineNumber = 0;

            var nameCol = 0;
            var categoryCol = 1;
            var priceCol = 2;
            var stockCol = 3;
            var thresholdCol = 4;
            var visibleCol = 5;
            var descCol = -1;
            var imageCol = -1;

            using (var reader = new StreamReader(csvFile.OpenReadStream(), Encoding.UTF8))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    lineNumber++;

                    var values = ParseCsvLine(line);
                    if (values.Count < 3) continue;

                    // Header row detection
                    if (lineNumber == 1)
                    {
                        var lowerValues = values.Select(v => v.ToLowerInvariant().Replace(" ", "").Replace("_", "")).ToList();
                        if (lowerValues.Any(v => v.Contains("name") || v.Contains("title") || v.Contains("price") || v.Contains("category")))
                        {
                            nameCol = lowerValues.FindIndex(v => v.Contains("name") || v.Contains("title"));
                            if (nameCol == -1) nameCol = 0;

                            categoryCol = lowerValues.FindIndex(v => v.Contains("cat"));
                            if (categoryCol == -1) categoryCol = 1;

                            priceCol = lowerValues.FindIndex(v => v.Contains("price") || v.Contains("cost") || v.Contains("rate"));
                            if (priceCol == -1) priceCol = 2;

                            stockCol = lowerValues.FindIndex(v => v.Contains("stock") || v.Contains("qty") || v.Contains("quantity"));
                            if (stockCol == -1) stockCol = 3;

                            thresholdCol = lowerValues.FindIndex(v => v.Contains("threshold") || v.Contains("lowstock"));
                            if (thresholdCol == -1) thresholdCol = 4;

                            visibleCol = lowerValues.FindIndex(v => v.Contains("visible") || v.Contains("active") || v.Contains("status"));
                            if (visibleCol == -1) visibleCol = 5;

                            descCol = lowerValues.FindIndex(v => v.Contains("desc") || v.Contains("detail"));
                            imageCol = lowerValues.FindIndex(v => v.Contains("image") || v.Contains("img") || v.Contains("photo") || v.Contains("url"));
                            if (imageCol == -1 && values.Count > 6 && descCol != 6) imageCol = values.Count - 1;

                            continue; // Skip header row
                        }
                    }

                    try
                    {
                        var rawName = nameCol >= 0 && nameCol < values.Count ? values[nameCol] : "";
                        var rawCat = categoryCol >= 0 && categoryCol < values.Count ? values[categoryCol] : "Stationery";
                        var rawPrice = priceCol >= 0 && priceCol < values.Count ? values[priceCol] : "0";
                        var rawStock = stockCol >= 0 && stockCol < values.Count ? values[stockCol] : "10";
                        var rawThreshold = thresholdCol >= 0 && thresholdCol < values.Count ? values[thresholdCol] : "5";
                        var rawVisible = visibleCol >= 0 && visibleCol < values.Count ? values[visibleCol] : "true";
                        var rawImg = imageCol >= 0 && imageCol < values.Count ? values[imageCol] : "";

                        var name = rawName.Trim('\"', '\'', ' ');
                        var cat = string.IsNullOrWhiteSpace(rawCat) ? "Stationery" : rawCat.Trim('\"', '\'', ' ');

                        // Clean price string (e.g. "$19.99" -> 19.99)
                        var cleanPriceStr = rawPrice.Replace("$", "").Replace("₹", "").Replace("€", "").Trim('\"', '\'', ' ');
                        if (!decimal.TryParse(cleanPriceStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
                        {
                            decimal.TryParse(cleanPriceStr, out price);
                        }

                        var cleanStockStr = rawStock.Trim('\"', '\'', ' ');
                        int.TryParse(cleanStockStr, out var stock);

                        var cleanThresholdStr = rawThreshold.Trim('\"', '\'', ' ');
                        if (!int.TryParse(cleanThresholdStr, out var threshold)) threshold = 5;

                        var cleanVisibleStr = rawVisible.Trim('\"', '\'', ' ').ToLowerInvariant();
                        var visible = cleanVisibleStr == "true" || cleanVisibleStr == "1" || cleanVisibleStr == "yes" || string.IsNullOrWhiteSpace(cleanVisibleStr);

                        var finalImgUrl = _cloudinaryService.ProcessImageUrl(rawImg.Trim('\"', '\'', ' '), cat, name);

                        if (!string.IsNullOrWhiteSpace(name) && price > 0)
                        {
                            var product = new Product
                            {
                                Name = name,
                                Category = ProductService.NormalizeCategory(cat),
                                Price = price,
                                StockQuantity = stock,
                                LowStockThreshold = threshold,
                                IsVisible = visible,
                                ImagePath = finalImgUrl,
                                AdminId = currentAdmin?.Id,
                                AdminUsername = currentAdmin?.Username
                            };

                            products.Add(product);
                            bulkModels.Add(new BulkProductModel
                            {
                                Name = name,
                                Category = cat,
                                Price = price,
                                StockQuantity = stock,
                                LowStockThreshold = threshold,
                                IsVisible = visible,
                                ImageUrl = finalImgUrl
                            });
                        }
                        else
                        {
                            errors.Add($"Row {lineNumber}: Skipped invalid item (Name: '{name}', Price: {price}).");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {lineNumber}: {ex.Message}");
                    }
                }
            }

            if (!products.Any())
            {
                return BadRequest(new { 
                    message = "No valid products found in the uploaded CSV file. Please verify the format.",
                    errors 
                });
            }

            try
            {
                await _db.Products.AddRangeAsync(products);
                await _db.SaveChangesAsync();

                // Non-blocking background sync
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _redisCache.RemoveByPatternAsync("products:*");
                        var all = await _db.Products.AsNoTracking().ToListAsync();
                        await _fallbackQueue.SaveProductCacheAsync(all);
                    }
                    catch { }
                });

                _eventStream.BroadcastEvent("stock_update", new
                {
                    action = "bulk_csv_import",
                    importedCount = products.Count,
                    timestamp = DateTime.UtcNow
                });

                return Ok(new
                {
                    success = true,
                    importedCount = products.Count,
                    errors,
                    message = $"Successfully imported {products.Count} products into database."
                });
            }
            catch (Exception ex)
            {
                await _fallbackQueue.EnqueuePendingActionAsync("bulk_create", bulkModels);

                return Ok(new
                {
                    success = true,
                    isQueued = true,
                    importedCount = products.Count,
                    errors,
                    message = $"Products queued to resilient fallback queue for auto-sync: {ex.Message}"
                });
            }
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line)) return result;

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        sb.Append('\"');
                        i++; // Skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            result.Add(sb.ToString().Trim());
            return result;
        }

        [HttpGet("download-csv-template")]
        public IActionResult DownloadCsvTemplate()
        {
            var csv = "Name,Category,Price,StockQuantity,LowStockThreshold,IsVisible,Description,ImageUrl\n" +
                      "\"Executive Leather Hardcover Notebook\",\"Notebooks\",19.99,60,10,true,\"Premium 120gsm ivory ruled paper journal with dual ribbon markers and magnetic clasp\",\"https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=600&auto=format&fit=crop\"\n" +
                      "\"Vintage Fountain Pen - Matte Black\",\"Writing\",28.50,40,8,true,\"Fine nib brass body fountain pen with smooth ink flow and converter included\",\"https://images.unsplash.com/photo-1583485088034-697b5bc54ccd?w=600&auto=format&fit=crop\"\n" +
                      "\"Pastel Morandi Gel Pens (10-Pack)\",\"Writing\",11.99,120,15,true,\"0.5mm quick-drying smudge-proof pastel color ink pens for study notes and journaling\",\"https://images.unsplash.com/photo-1585336261026-7f4153b6d773?w=600&auto=format&fit=crop\"\n" +
                      "\"Dual-Sided Leather Desk Mat\",\"Desk Accessories\",24.99,35,5,true,\"Waterproof anti-slip extended mouse pad and writing blotter for modern workspaces\",\"https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=600&auto=format&fit=crop\"\n" +
                      "\"Pastel Sticky Notes Cube (600 Sheets)\",\"Desk Accessories\",7.49,150,20,true,\"Color-coded removable adhesive notes for agile task boards and reminders\",\"https://images.unsplash.com/photo-1586075010923-2dd4570fb338?w=600&auto=format&fit=crop\"";

            var bytes = Encoding.UTF8.GetBytes(csv);
            return File(bytes, "text/csv", "sample_products.csv");
        }

        [HttpPost("inventory-upload")]
        public async Task<IActionResult> InventoryUpload(IFormFile file, [FromQuery] int? defaultThreshold)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Please upload an inventory receipt/image file." });
            }

            var service = new OcrInventoryService();
            string? tessPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX");
            var items = service.ExtractItems(file, tessPath, out string ocrMessage);

            int created = 0, updated = 0;
            int threshold = defaultThreshold.HasValue ? Math.Max(0, defaultThreshold.Value) : 5;

            if (items != null && items.Any())
            {
                foreach (var it in items)
                {
                    var existing = await _db.Products.FirstOrDefaultAsync(p => p.Name.ToLower() == it.ProductName.ToLower());
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
                await _db.SaveChangesAsync();

                _eventStream.BroadcastEvent("stock_update", new
                {
                    action = "ocr_upload",
                    created,
                    updated,
                    timestamp = DateTime.UtcNow
                });
            }

            return Ok(new
            {
                success = true,
                message = string.IsNullOrEmpty(ocrMessage)
                    ? $"Processed {items?.Count ?? 0} items. Created {created}, Updated {updated}."
                    : ocrMessage,
                created,
                updated,
                extractedItems = items ?? new List<OcrInventoryItem>()
            });
        }
    }
}
