using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace ShopHangTet.Models;

/// <summary>
/// Giỏ quà tùy chỉnh (Mix & Match)
/// </summary>
public class CustomBasket
{
    [Key]
    public ObjectId Id { get; set; }
    
    public ObjectId BasketTemplateId { get; set; }
    
    // Danh sách thành phần đã chọn
    public List<BasketItemSelection> Items { get; set; } = new();
    
    public decimal TotalPrice { get; set; }
    
    // Gifting options
    public string GreetingMessage { get; set; } = string.Empty;
    public string CanvaCardLink { get; set; } = string.Empty; // Link thiệp Canva
    public bool HideInvoice { get; set; } = false; // Ẩn hóa đơn khi giao
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Món đã chọn trong giỏ custom
/// </summary>
public class BasketItemSelection
{
    public ObjectId ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
