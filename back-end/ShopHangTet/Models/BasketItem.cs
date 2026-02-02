using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace ShopHangTet.Models;

public class BasketItem
{
    [Key]
    public ObjectId Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // "Wine", "Cake", "Tea", "Fruit", etc.
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Ý nghĩa/Thông điệp
    public List<string> Meanings { get; set; } = new(); // ["Corporate", "Family", "Health", "Prosperity"]
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
