# ==============================================================================
# Multi-Stage Dockerfile for .NET 8 Web API (Root Level for Render / Cloud)
# ==============================================================================

# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["Stationary/Stationary.csproj", "Stationary/"]
RUN dotnet restore "Stationary/Stationary.csproj"

# Copy full source and publish
COPY Stationary/ Stationary/
WORKDIR "/src/Stationary"
RUN dotnet publish "Stationary.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install native Linux libraries for PDF generation and OCR
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    fontconfig \
    libfontconfig1 \
    tesseract-ocr \
    tesseract-ocr-eng \
    libtesseract-dev \
    curl \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Environment & Server Port Configuration
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Database & Redis Connection Strings
ENV ConnectionStrings__DefaultConnection="Host=aws-0-ap-northeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.uzrfkqosqndjzkzgulrf;Password=Akash@875669;SSL Mode=Require;Trust Server Certificate=true;"
ENV ConnectionStrings__Redis="striking-titmouse-129181.upstash.io:6379,password=gQAAAAAAAfidAAIgcDFkMTk5MmM2ZGRjYjI0MGIzYjA4ZWM0MmU1YjFjNTk1Mw,ssl=True,abortConnect=false"

# Third-party Integrations & JWT Secrets
ENV Upstash__RestUrl="https://striking-titmouse-129181.upstash.io"
ENV Upstash__RestToken="gQAAAAAAAfidAAIgcDFkMTk5MmM2ZGRjYjI0MGIzYjA4ZWM0MmU1YjFjNTk1Mw"
ENV Cloudinary__CloudName="dlbtwfubs"
ENV Cloudinary__ApiKey="555784333712377"
ENV Cloudinary__ApiSecret="BetEGd6UbOiY3JePeUmJQjMiFE4"
ENV Supabase__Url="https://uzrfkqosqndjzkzgulrf.supabase.co"
ENV Supabase__AnonKey="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV6cmZrcW9zcW5kanpremd1bHJmIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc4NzU4NDk1NywiZXhwIjoyMTAzMTYwOTU3fQ.Dv68LaJlN3iH8GtDerqsORo30YNdXAWeEEo7zaTA4YA"
ENV Jwt__Secret="StationarySystemSecretKey_SuperSecureKey_2026!"

EXPOSE 5000
EXPOSE 8080
EXPOSE 10000

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:${PORT:-5000}/health || exit 1

ENTRYPOINT ["dotnet", "Stationary.dll"]
