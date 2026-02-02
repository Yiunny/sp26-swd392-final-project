using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace ShopHangTet.Models;

public class BasketTemplate
{
    [Key]
    public ObjectId Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Kích thước/Loại vỏ
    public string Size { get; set; } = string.Empty; // "Small", "Medium", "Large", "Premium"
    public int MaxItems { get; set; } = 10; // Số lượng món tối đa có thể chứa
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
