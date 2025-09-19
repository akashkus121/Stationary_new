using System.ComponentModel.DataAnnotations;

namespace Stationary.Models
{
    public class StockManagementViewModel
    {
        public List<Product> OutOfStockProducts { get; set; } = new List<Product>();
        public List<Product> LowStockProducts { get; set; } = new List<Product>();
        public List<Product> AllProducts { get; set; } = new List<Product>();

        [Display(Name = "Stock Alert Threshold")]
        [Range(1, 100, ErrorMessage = "Threshold must be between 1 and 100")]
        public int GlobalLowStockThreshold { get; set; } = 10;

        [Display(Name = "Auto-hide Out of Stock")]
        public bool AutoHideOutOfStock { get; set; } = true;

        [Display(Name = "Show Low Stock Warnings")]
        public bool ShowLowStockWarnings { get; set; } = true;
    }
}