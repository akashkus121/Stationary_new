# 🏪 LocalLifePlus Dashboard - Stationary Management System

A comprehensive web-based inventory management system for stationary products, built with ASP.NET Core MVC. This system provides both admin and user interfaces for managing products, inventory, and orders.

![Dashboard Preview](https://img.shields.io/badge/Status-Active-brightgreen) ![.NET Version](https://img.shields.io/badge/.NET-6.0-blue) ![Database](https://img.shields.io/badge/Database-SQL%20Server-orange)

## 🌟 Features

### 👨‍💼 Admin Features
- **Product Management**: Add, edit, delete, and view products
- **Bulk Product Creation**: Create multiple products at once using forms or CSV upload
- **Inventory Management**: Track stock levels and low stock alerts
- **Stock Visibility Control**: Show/hide products based on availability
- **Reports & Analytics**: View sales reports and inventory statistics
- **OCR Inventory Upload**: Upload inventory using image recognition
- **User Management**: Manage user accounts and roles

### 👤 User Features
- **Product Catalog**: Browse available products with search and filtering
- **Shopping Cart**: Add products to cart with stock validation
- **Stock-Aware Cart**: Prevents adding more items than available in stock
- **Real-time Stock Display**: See current stock levels for each product
- **Category Filtering**: Filter products by category
- **Responsive Design**: Works on desktop and mobile devices

### 🔧 Technical Features
- **Stock Validation**: Prevents overselling with real-time stock checks
- **CSV Import/Export**: Bulk data management capabilities
- **Image Upload**: Product image management
- **Session Management**: Secure user authentication
- **Responsive UI**: Modern, mobile-friendly interface
- **Error Handling**: Comprehensive error management and user feedback

## 🚀 Quick Start

### Prerequisites
- .NET 6.0 SDK or later
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/LocalLifePlusDashboard.git
   cd LocalLifePlusDashboard/Stationary
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Update connection string**
   - Open `appsettings.json`
   - Update the connection string to point to your SQL Server instance:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StationaryDB;Trusted_Connection=true;MultipleActiveResultSets=true"
     }
   }
   ```

4. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   - Open your browser and navigate to `https://localhost:5001`
   - Default admin credentials: `admin` / `admin123`

## 📁 Project Structure

```
Stationary/
├── Controllers/           # MVC Controllers
│   ├── AdminController.cs # Admin functionality
│   ├── UserController.cs  # User functionality
│   └── AccountController.cs # Authentication
├── Models/               # Data Models
│   ├── Product.cs        # Product entity
│   ├── Cart.cs          # Shopping cart
│   ├── User.cs          # User entity
│   └── BulkProductModel.cs # Bulk creation model
├── Views/               # Razor Views
│   ├── Admin/           # Admin interface
│   ├── User/            # User interface
│   └── Shared/          # Shared layouts
├── Services/            # Business Logic
│   ├── ProductService.cs
│   ├── CartService.cs
│   └── OcrInventoryService.cs
├── Data/               # Database related
│   ├── ApplicationDbContext.cs
│   └── DatabaseSetup/   # SQL scripts
└── wwwroot/            # Static files
    ├── css/            # Stylesheets
    ├── js/             # JavaScript files
    └── images/         # Product images
```

## 🎯 Key Features in Detail

### 📦 Product Management
- **Single Product Creation**: Traditional form-based product addition
- **Bulk Product Creation**: 
  - Form-based multiple product entry
  - CSV file upload with template download
  - Sample product generator
- **Product Editing**: Update product details, prices, and stock
- **Image Management**: Upload and manage product images

### 🛒 Shopping Cart System
- **Stock Validation**: Real-time stock checking prevents overselling
- **Quantity Limits**: Users cannot add more items than available in stock
- **Visual Stock Display**: Clear indication of available stock
- **Cart Management**: Add, remove, and update quantities

### 📊 Inventory Management
- **Stock Tracking**: Real-time stock level monitoring
- **Low Stock Alerts**: Automatic notifications for low inventory
- **Stock Visibility**: Control which products are visible to users
- **Bulk Stock Updates**: Update multiple products at once

### 📈 Reporting & Analytics
- **Sales Reports**: Track product performance
- **Inventory Reports**: Stock level analysis
- **User Activity**: Monitor user interactions

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 6.0 MVC
- **Database**: SQL Server with Entity Framework Core
- **Frontend**: HTML5, CSS3, JavaScript, jQuery
- **UI Framework**: Custom CSS with responsive design
- **Authentication**: Session-based authentication
- **File Processing**: CSV import/export, image upload

## 📱 Screenshots

### Admin Dashboard
![Admin Dashboard](https://via.placeholder.com/800x400/4b6cff/ffffff?text=Admin+Dashboard)

### Product Management
![Product Management](https://via.placeholder.com/800x400/28a745/ffffff?text=Product+Management)

### Bulk Creation Interface
![Bulk Creation](https://via.placeholder.com/800x400/e74c3c/ffffff?text=Bulk+Product+Creation)

### User Shopping Interface
![User Interface](https://via.placeholder.com/800x400/17a2b8/ffffff?text=User+Shopping+Interface)

## 🔧 Configuration

### Database Setup
The application uses Entity Framework Core with SQL Server. Database scripts are included in the `Data/DatabaseSetup/` folder.

### Environment Variables
- `TESSDATA_PREFIX`: Path to Tesseract data files (for OCR functionality)

### File Uploads
- Product images are stored in `wwwroot/images/`
- CSV files are processed temporarily during upload

## 🚀 Deployment

### Local Development
1. Ensure SQL Server is running
2. Update connection string in `appsettings.json`
3. Run `dotnet ef database update`
4. Start the application with `dotnet run`

### Production Deployment
1. Update connection string for production database
2. Configure IIS or Azure App Service
3. Set up SSL certificates
4. Configure environment variables

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **Akash Kushwaha** - *Initial work* - [YourGitHub](https://github.com/akashkus121)

## 🙏 Acknowledgments

- ASP.NET Core team for the excellent framework
- Entity Framework team for the ORM
- All contributors who helped improve this project

## 📞 Support

If you have any questions or need help with the project, please:
- Open an issue on GitHub
- Contact us at [908akashkushwaha@gmial.com]
- Check the documentation in the `/docs` folder

## 🔄 Version History

- **v1.0.0** - Initial release with basic product management
- **v1.1.0** - Added bulk product creation
- **v1.2.0** - Implemented stock validation and cart system
- **v1.3.0** - Added CSV import/export functionality
- **v1.4.0** - Enhanced UI and mobile responsiveness

---

⭐ **Star this repository if you found it helpful!**

[![GitHub stars](https://img.shields.io/github/stars/yourusername/LocalLifePlusDashboard?style=social)](https://github.com/yourusername/LocalLifePlusDashboard/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/yourusername/LocalLifePlusDashboard?style=social)](https://github.com/yourusername/LocalLifePlusDashboard/network)
[![GitHub issues](https://img.shields.io/github/issues/yourusername/LocalLifePlusDashboard)](https://github.com/yourusername/LocalLifePlusDashboard/issues)
