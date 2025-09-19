using System.ComponentModel.DataAnnotations;

namespace Stationary.Models
{
    public class StockUpdateModel
    {
        public int ProductId { get; set; }
        
        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int NewStockQuantity { get; set; }
        
        [Range(0, int.MaxValue, ErrorMessage = "Low stock threshold cannot be negative")]
        public int NewLowStockThreshold { get; set; }
        
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int CurrentLowStockThreshold { get; set; }
    }
}