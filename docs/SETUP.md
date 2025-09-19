# LocalLifePlus Dashboard - Setup Guide

## 🚀 Quick Setup (5 minutes)

### Prerequisites
- Windows 10/11, macOS, or Linux
- .NET 6.0 SDK or later
- SQL Server (LocalDB is fine for development)
- Git

### Step 1: Clone the Repository
```bash
git clone https://github.com/yourusername/LocalLifePlusDashboard.git
cd LocalLifePlusDashboard/Stationary
```

### Step 2: Install Dependencies
```bash
dotnet restore
```

### Step 3: Configure Database
1. Open `appsettings.json`
2. Update the connection string if needed:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StationaryDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### Step 4: Run Database Migrations
```bash
dotnet ef database update
```

### Step 5: Run the Application
```bash
dotnet run
```

### Step 6: Access the Application
- Open your browser
- Navigate to `https://localhost:5001`
- Default admin login: `admin` / `admin123`

## 🔧 Detailed Setup Instructions

### Development Environment Setup

#### Option 1: Visual Studio 2022
1. Install Visual Studio 2022 with ASP.NET workload
2. Open `Stationary.sln`
3. Press F5 to run

#### Option 2: Visual Studio Code
1. Install VS Code with C# extension
2. Open the project folder
3. Press F5 to run

#### Option 3: Command Line
```bash
dotnet run --urls="https://localhost:5001"
```

### Database Setup

#### SQL Server LocalDB (Recommended for Development)
1. Install SQL Server LocalDB (comes with Visual Studio)
2. No additional configuration needed
3. Database will be created automatically

#### SQL Server Express/Full
1. Install SQL Server Express or Full
2. Update connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=StationaryDB;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

#### SQL Server with Username/Password
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=StationaryDB;User Id=yourusername;Password=yourpassword;MultipleActiveResultSets=true"
  }
}
```

### Environment Configuration

#### Development
- Uses `appsettings.Development.json`
- Detailed error pages enabled
- Hot reload enabled

#### Production
- Uses `appsettings.Production.json`
- Error pages simplified
- Logging configured

### File Upload Configuration

#### Product Images
- Images stored in `wwwroot/images/`
- Supported formats: JPG, PNG, GIF
- Maximum file size: 5MB (configurable)

#### CSV Uploads
- Temporary processing in memory
- Supported format: CSV with headers
- Maximum file size: 10MB

## 🐛 Troubleshooting

### Common Issues

#### Database Connection Issues
**Error**: `Cannot connect to database`
**Solution**:
1. Ensure SQL Server is running
2. Check connection string
3. Verify database exists
4. Run `dotnet ef database update`

#### Port Already in Use
**Error**: `Port 5001 is already in use`
**Solution**:
```bash
dotnet run --urls="https://localhost:5002"
```

#### Package Restore Issues
**Error**: `Package restore failed`
**Solution**:
```bash
dotnet clean
dotnet restore
```

#### Entity Framework Issues
**Error**: `No database provider configured`
**Solution**:
1. Check `Startup.cs` or `Program.cs`
2. Ensure `AddDbContext` is called
3. Verify connection string

### Performance Issues

#### Slow Database Queries
1. Check database indexes
2. Use SQL Server Profiler
3. Optimize LINQ queries
4. Consider stored procedures

#### Memory Issues
1. Check for memory leaks
2. Optimize image processing
3. Implement caching
4. Monitor garbage collection

## 🔒 Security Configuration

### Authentication
- Session-based authentication
- Password hashing with BCrypt
- Role-based authorization

### HTTPS Configuration
1. Install SSL certificate
2. Update `launchSettings.json`:
```json
{
  "applicationUrl": "https://localhost:5001;http://localhost:5000"
}
```

### Environment Variables
Set sensitive data as environment variables:
```bash
export ConnectionStrings__DefaultConnection="your-connection-string"
export AdminPassword="your-admin-password"
```

## 📊 Monitoring Setup

### Logging Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Performance Monitoring
1. Enable Application Insights
2. Configure health checks
3. Set up monitoring alerts

## 🚀 Deployment

### Local IIS
1. Publish the application:
```bash
dotnet publish -c Release
```
2. Deploy to IIS
3. Configure application pool
4. Set up SSL certificate

### Azure App Service
1. Create Azure App Service
2. Deploy from Visual Studio
3. Configure connection strings
4. Set up custom domain

### Docker (Optional)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY . /app
WORKDIR /app
EXPOSE 80
ENTRYPOINT ["dotnet", "Stationary.dll"]
```

## 🧪 Testing Setup

### Unit Testing
```bash
dotnet test
```

### Integration Testing
1. Create test database
2. Run integration tests
3. Clean up test data

### Load Testing
1. Use tools like Apache JMeter
2. Test with multiple users
3. Monitor performance metrics

## 📝 Configuration Files

### appsettings.json
Main configuration file with:
- Connection strings
- Logging settings
- Application settings

### launchSettings.json
Development launch configuration:
- URLs and ports
- Environment variables
- Launch profiles

### Program.cs / Startup.cs
Application startup configuration:
- Service registration
- Middleware pipeline
- Database context setup

## 🔄 Updates and Maintenance

### Updating Dependencies
```bash
dotnet list package --outdated
dotnet add package PackageName --version LatestVersion
```

### Database Migrations
```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Backup and Restore
1. Regular database backups
2. Configuration file backups
3. Image file backups
4. Disaster recovery plan

## 📞 Support

### Getting Help
1. Check this documentation
2. Search GitHub issues
3. Create a new issue
4. Contact maintainers

### Common Commands
```bash
# Restore packages
dotnet restore

# Build project
dotnet build

# Run application
dotnet run

# Run tests
dotnet test

# Update database
dotnet ef database update

# Create migration
dotnet ef migrations add MigrationName
```

## 🎯 Next Steps

After setup:
1. Create your first product
2. Test bulk creation
3. Configure user accounts
4. Set up reporting
5. Customize the interface

Happy coding! 🚀

