using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using QuestPDF.Infrastructure;
using Stationary.Services;


var builder = WebApplication.CreateBuilder(args);

// Set QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// Add services
builder.Services.AddControllersWithViews();

// Register ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register custom services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IStoredProcedureService, StoredProcedureService>();

// Add health checks
builder.Services.AddHealthChecks();

// Add session + cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add global exception handling middleware
app.UseMiddleware<Stationary.Middleware.ExceptionHandlingMiddleware>();

// Prevent browser from caching dynamic pages (mitigates back button showing protected pages)
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var path = context.Request.Path.Value?.ToLower();
        if (!string.IsNullOrEmpty(path) &&
            (path.StartsWith("/lib") || path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/images") || path.StartsWith("/favicon")))
        {
            return Task.CompletedTask;
        }

        context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
        return Task.CompletedTask;
    });

    await next();
});

// ✅ Session must come BEFORE Authorization
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map health checks
app.MapHealthChecks("/health");

app.Run();
