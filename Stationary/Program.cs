using Microsoft.EntityFrameworkCore;
using Stationary.Data;
using QuestPDF.Infrastructure;
using Stationary.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

// Enable legacy timestamp behavior for PostgreSQL/Supabase compatibility
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Set QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// Add services for API
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Register ApplicationDbContext with Supabase PostgreSQL (Npgsql) with auto-region resolver
var rawConnStr = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=aws-0-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres;Password=your_password;SSL Mode=Require;Trust Server Certificate=true;";

var resolvedConnStr = ResolveSupabaseConnectionString(rawConnStr);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(resolvedConnStr));

// Register Redis Connection with resilient fallback
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false,connectTimeout=5000";
try
{
    var redisMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);
}
catch (Exception ex)
{
    Console.WriteLine($"[Redis Notice] Unable to connect to Redis at {redisConnectionString}: {ex.Message}. Resilient in-memory fallback cache will be used.");
}

// Register Redis Cache Service
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

// Register custom services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IStoredProcedureService, StoredProcedureService>();
builder.Services.AddSingleton<IEventStreamService, EventStreamService>();
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();
builder.Services.AddSingleton<IOfflineFallbackQueueService, OfflineFallbackQueueService>();
builder.Services.AddSingleton<IProductLockService, ProductLockService>();
builder.Services.AddHostedService<PendingQueueProcessorService>();

// Support dynamic PORT assigned by hosting providers like Render
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add CORS policy for React Frontend (Local + Hosted)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "StationarySystemSecretKey_SuperSecureKey_2026!";
var key = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

// Add Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Stationary Management API (Supabase + Redis)", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Stationary API v1");
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseCors("AllowReactApp");

app.UseRouting();

// Add global exception handling middleware
app.UseMiddleware<Stationary.Middleware.ExceptionHandlingMiddleware>();

// Prevent browser from caching dynamic pages
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

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure PostgreSQL / Supabase Schema and Initial Admin Seed
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Auto-create tables if they don't exist
        db.Database.EnsureCreated();

        try
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS ""Carts"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    ""UserId"" integer NOT NULL,
                    ""ProductId"" integer NOT NULL,
                    ""Quantity"" integer NOT NULL,
                    ""AddedDate"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""UpdatedDate"" timestamp with time zone NULL
                );

                CREATE TABLE IF NOT EXISTS ""Orders"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    ""UserId"" integer NOT NULL,
                    ""Date"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    ""TotalAmount"" numeric(18,2) NOT NULL DEFAULT 0.00,
                    ""Subtotal"" numeric(18,2) NOT NULL DEFAULT 0.00,
                    ""TaxAmount"" numeric(18,2) NOT NULL DEFAULT 0.00,
                    ""PaymentMethod"" text NOT NULL DEFAULT 'cash',
                    ""OrderStatus"" character varying(50) NOT NULL DEFAULT 'Pending',
                    ""Notes"" text NULL,
                    ""UpdatedDate"" timestamp with time zone NULL
                );

                CREATE TABLE IF NOT EXISTS ""OrderItems"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    ""OrderId"" integer NOT NULL,
                    ""ProductId"" integer NOT NULL,
                    ""AdminId"" integer NULL,
                    ""ProductName"" text NOT NULL DEFAULT '',
                    ""Quantity"" integer NOT NULL,
                    ""Price"" numeric(18,2) NOT NULL DEFAULT 0.00,
                    ""TotalPrice"" numeric(18,2) NOT NULL DEFAULT 0.00
                );

                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'RefreshToken') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""RefreshToken"" text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Users' AND column_name = 'RefreshTokenExpiryTime') THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""RefreshTokenExpiryTime"" timestamp with time zone NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'AdminId') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""AdminId"" integer NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'AdminUsername') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""AdminUsername"" character varying(100) NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'Description') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""Description"" text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'CreatedDate') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""CreatedDate"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'IsActive') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""IsActive"" boolean NOT NULL DEFAULT true;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Products' AND column_name = 'UpdatedDate') THEN
                        ALTER TABLE ""Products"" ADD COLUMN ""UpdatedDate"" timestamp with time zone NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Carts' AND column_name = 'AddedDate') THEN
                        ALTER TABLE ""Carts"" ADD COLUMN ""AddedDate"" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Carts' AND column_name = 'UpdatedDate') THEN
                        ALTER TABLE ""Carts"" ADD COLUMN ""UpdatedDate"" timestamp with time zone NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'OrderItems' AND column_name = 'AdminId') THEN
                        ALTER TABLE ""OrderItems"" ADD COLUMN ""AdminId"" integer NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Orders' AND column_name = 'Notes') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""Notes"" text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Orders' AND column_name = 'OrderStatus') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""OrderStatus"" character varying(50) NOT NULL DEFAULT 'Pending';
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Orders' AND column_name = 'UpdatedDate') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""UpdatedDate"" timestamp with time zone NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Orders' AND column_name = 'Subtotal') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""Subtotal"" numeric(18,2) NOT NULL DEFAULT 0.00;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Orders' AND column_name = 'TaxAmount') THEN
                        ALTER TABLE ""Orders"" ADD COLUMN ""TaxAmount"" numeric(18,2) NOT NULL DEFAULT 0.00;
                    END IF;
                END $$;
            ");
        }
        catch { }

        // Seed default admin user (akash / 12345)
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<Stationary.Models.User>();
        var akash = db.Users.FirstOrDefault(u => u.Username.ToLower() == "akash");
        if (akash == null)
        {
            akash = new Stationary.Models.User
            {
                Username = "akash",
                Role = "Admin"
            };
            akash.Password = hasher.HashPassword(akash, "12345");
            db.Users.Add(akash);
            db.SaveChanges();
        }

        // Seed default test user (test / 12345)
        var testUser = db.Users.FirstOrDefault(u => u.Username.ToLower() == "test");
        if (testUser == null)
        {
            testUser = new Stationary.Models.User
            {
                Username = "test",
                Role = "User"
            };
            testUser.Password = hasher.HashPassword(testUser, "12345");
            db.Users.Add(testUser);
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database Setup Notice] {ex.Message}");
    }
}

// Map health checks
app.MapHealthChecks("/health");

app.Run();

static string ResolveSupabaseConnectionString(string? initialConnStr)
{
    if (string.IsNullOrWhiteSpace(initialConnStr)) return initialConnStr ?? "";

    try
    {
        using var testConn = new Npgsql.NpgsqlConnection(initialConnStr + ";Timeout=4;");
        testConn.Open();
        Console.WriteLine("[Supabase] Direct connection successful!");
        return initialConnStr;
    }
    catch (Npgsql.PostgresException ex) when (ex.MessageText.Contains("ENOTFOUND") || ex.MessageText.Contains("tenant"))
    {
        Console.WriteLine("[Supabase Auto-Detect] Initial pooler region returned tenant not found. Detecting correct AWS region...");
        var regions = new[] { "ap-southeast-1", "us-east-1", "eu-central-1", "us-west-1", "eu-west-1", "ap-northeast-1", "ca-central-1", "ap-southeast-2", "sa-east-1", "ap-south-1" };
        var csb = new Npgsql.NpgsqlConnectionStringBuilder(initialConnStr);

        foreach (var r in regions)
        {
            csb.Host = $"aws-0-{r}.pooler.supabase.com";
            csb.Port = 6543;
            csb.Timeout = 4;
            try
            {
                using var conn = new Npgsql.NpgsqlConnection(csb.ConnectionString);
                conn.Open();
                Console.WriteLine($"[Supabase Auto-Detect] >>> Successfully connected to region: {r} ({csb.Host}) <<<");
                csb.Timeout = 30;
                return csb.ConnectionString;
            }
            catch { }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Supabase Notice] {ex.Message}");
    }

    return initialConnStr;
}
