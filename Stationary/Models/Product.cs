using System.ComponentModel.DataAnnotations;

namespace Stationary.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        public string? ImagePath { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Low stock threshold cannot be negative")]
        public int LowStockThreshold { get; set; }

        // Computed property to check if product is out of stock
        public bool IsOutOfStock => StockQuantity <= 0;

        // Computed property to check if product is low in stock
        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= LowStockThreshold;

        // Computed property to get stock status
        public string StockStatus
        {
            get
            {
                if (IsOutOfStock) return "Out of Stock";
                if (IsLowStock) return $"Low Stock ({StockQuantity} left)";
                return $"In Stock ({StockQuantity} available)";
            }
        }

        // Property to control product visibility
        public bool IsVisible { get; set; } = true;

        // Additional properties for stored procedures and Multi-Admin ownership
        public int? AdminId { get; set; }
        public string? AdminUsername { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Computed property for effective visibility (considering stock and visibility settings)
        // If IsVisible is explicitly set to true, always show the product regardless of stock
        // If IsVisible is false, hide the product regardless of stock
        // This gives you full control over product visibility
        public bool IsEffectivelyVisible => IsVisible;
        
        // Static property for global settings (could be moved to configuration)
        public static bool AutoHideOutOfStock { get; set; } = true;
    }
}
