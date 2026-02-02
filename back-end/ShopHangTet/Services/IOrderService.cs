using ShopHangTet.DTOs;
using ShopHangTet.Models;

namespace ShopHangTet.Services
{
    /// <summary>
    /// Interface cho OrderService - quản lý đơn hàng B2C và B2B
    /// </summary>
    public interface IOrderService
    {
        // === Order Placement (Legacy - for compatibility) ===
        Task<OrderModel> PlaceOrderAsync(CreateOrderDto dto);
        Task<OrderModel> PlaceB2BOrderAsync(CreateOrderDto dto);
        
        // === Order Validation ===
        Task<OrderValidationResult> ValidateOrderAsync(CreateOrderDto dto);
        
        // === Order Tracking ===
        Task<OrderTrackingResult?> TrackOrderAsync(string orderCode, string email);
    }

    /// <summary>
    /// DTO tạo đơn hàng (dùng trong OrderService)
    /// </summary>
    public class CreateOrderDto
    {
        public string? UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        
        public List<OrderItemRequestDto> Items { get; set; } = new();
        public List<DeliveryAddressRequestDto> DeliveryAddresses { get; set; } = new();
        
        public string? DeliverySlotId { get; set; }
        public string PaymentMethod { get; set; } = "COD";
        
        public OrderTypeDto OrderType { get; set; } = OrderTypeDto.B2C;
    }

    public class OrderItemRequestDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? CustomBasketId { get; set; }
    }

    public class DeliveryAddressRequestDto
    {
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string AddressLine { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string GreetingMessage { get; set; } = string.Empty;
        public bool HideInvoice { get; set; }
    }

    public class OrderValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class OrderTrackingResult
    {
        public string OrderCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    }
}
