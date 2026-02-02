using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace ShopHangTet.Models;

/// <summary>
/// Đơn hàng (Hỗ trợ Guest & Authenticated, Multi-address)
/// </summary>
public class OrderModel
{
    [Key]
    public ObjectId Id { get; set; }
    
    // Order Code for tracking
    public string OrderCode { get; set; } = string.Empty;
    
    // Thông tin khách hàng
    public ObjectId? UserId { get; set; } // Null nếu Guest checkout
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    
    // Danh sách sản phẩm (có thể là custom basket hoặc pre-made)
    public List<OrderItem> Items { get; set; } = new();
    
    // Multi-address delivery
    public List<DeliveryAddress> DeliveryAddresses { get; set; } = new();
    
    // Scheduled delivery
    public ObjectId? DeliverySlotId { get; set; }
    public DateTime? ScheduledDeliveryDate { get; set; }
    public string? DeliveryTimeSlot { get; set; }
    
    // Pricing
    public decimal SubTotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Order status
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    // Payment
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
    public string? PaymentTransactionId { get; set; }
    
    // Tracking
    public List<OrderStatusHistory> StatusHistory { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    public ObjectId ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty; // "CustomBasket", "PreMadeBasket", "SingleItem"
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    
    // Nếu là custom basket
    public ObjectId? CustomBasketId { get; set; }
}

public class DeliveryAddress
{
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty; // Phường/Xã - Quan trọng cho tính phí ship
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    
    // Gifting options per address
    public string GreetingMessage { get; set; } = string.Empty;
    public bool HideInvoice { get; set; } = false;
}

public class OrderStatusHistory
{
    public OrderStatus Status { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty; // Admin/System
    public string Notes { get; set; } = string.Empty;
}

public enum OrderStatus
{
    Pending,          // Chờ xử lý
    PaymentConfirmed, // Đã thanh toán
    Processing,       // Đang đóng gói
    Shipping,         // Đang giao
    Delivered,        // Đã giao
    Cancelled,        // Đã hủy
    Refunded          // Đã hoàn tiền
}

public enum PaymentMethod
{
    COD,              // Tiền mặt
    BankTransfer,     // Chuyển khoản
    VNPAY,
    Momo,
    ZaloPay
}

public enum PaymentStatus
{
    Unpaid,
    Paid,
    Refunded,
    Failed
}
