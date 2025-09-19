using System.ComponentModel.DataAnnotations;

namespace Stationary.Models
{
    public class BulkProductModel
    {
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        public string? Category { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; } = 0;

        [Range(0, int.MaxValue, ErrorMessage = "Low stock threshold cannot be negative")]
        public int LowStockThreshold { get; set; } = 5;

        public bool IsVisible { get; set; } = true;
    }
}

