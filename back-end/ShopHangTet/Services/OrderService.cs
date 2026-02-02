using ShopHangTet.Interfaces;
using ShopHangTet.Models;
using ShopHangTet.DTOs;
using MongoDB.Bson;

namespace ShopHangTet.Services
{
    /// <summary>
    /// OrderService - Quản lý tạo và xử lý đơn hàng
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly IBasketItemRepository _itemRepo;
        private readonly IBasketTemplateRepository _templateRepo;
        private readonly IDeliverySlotRepository _slotRepo;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IBasketItemRepository itemRepo,
            IBasketTemplateRepository templateRepo,
            IDeliverySlotRepository slotRepo,
            ILogger<OrderService> logger)
        {
            _itemRepo = itemRepo;
            _templateRepo = templateRepo;
            _slotRepo = slotRepo;
            _logger = logger;
        }

        /// <summary>
        /// Đặt hàng B2C (Guest hoặc Member)
        /// </summary>
        public async Task<OrderModel> PlaceOrderAsync(CreateOrderDto dto)
        {
            _logger.LogInformation($"Creating B2C order for {dto.CustomerEmail}");

            // Generate order code
            var orderCode = GenerateOrderCode();

            // Create order
            var order = new OrderModel
            {
                Id = ObjectId.GenerateNewId(),
                OrderCode = orderCode,
                UserId = string.IsNullOrEmpty(dto.UserId) ? null : ObjectId.Parse(dto.UserId),
                CustomerName = dto.CustomerName,
                CustomerEmail = dto.CustomerEmail,
                CustomerPhone = dto.CustomerPhone,
                Items = dto.Items.Select(MapOrderItem).ToList(),
                DeliveryAddresses = dto.DeliveryAddresses.Select(MapDeliveryAddress).ToList(),
                DeliverySlotId = string.IsNullOrEmpty(dto.DeliverySlotId) ? null : ObjectId.Parse(dto.DeliverySlotId),
                SubTotal = CalculateSubTotal(dto.Items),
                ShippingFee = CalculateShippingFee(dto.DeliveryAddresses),
                Status = Models.OrderStatus.Pending,
                PaymentMethod = Enum.TryParse<PaymentMethod>(dto.PaymentMethod, out var pm) ? pm : PaymentMethod.COD,
                PaymentStatus = PaymentStatus.Unpaid,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            order.TotalAmount = order.SubTotal + order.ShippingFee;

            // Add initial status history
            order.StatusHistory.Add(new Models.OrderStatusHistory
            {
                Status = Models.OrderStatus.Pending,
                Timestamp = DateTime.UtcNow,
                UpdatedBy = "System",
                Notes = "Đơn hàng được tạo - Đang xác nhận thanh toán"
            });

            _logger.LogInformation($"Order created: {order.OrderCode}");
            return await Task.FromResult(order);
        }

        /// <summary>
        /// Đặt hàng B2B (Chỉ Member - nhiều địa chỉ)
        /// </summary>
        public async Task<OrderModel> PlaceB2BOrderAsync(CreateOrderDto dto)
        {
            if (string.IsNullOrEmpty(dto.UserId))
            {
                throw new InvalidOperationException("B2B order requires authenticated user");
            }

            _logger.LogInformation($"Creating B2B order for user {dto.UserId}");

            var order = await PlaceOrderAsync(dto);
            // B2B specific logic can be added here

            return order;
        }

        /// <summary>
        /// Validate đơn hàng trước khi tạo
        /// </summary>
        public async Task<OrderValidationResult> ValidateOrderAsync(CreateOrderDto dto)
        {
            var result = new OrderValidationResult { IsValid = true };

            // Validate required fields
            if (string.IsNullOrEmpty(dto.CustomerEmail))
            {
                result.Errors.Add("Email là bắt buộc");
                result.IsValid = false;
            }

            if (string.IsNullOrEmpty(dto.CustomerName))
            {
                result.Errors.Add("Tên khách hàng là bắt buộc");
                result.IsValid = false;
            }

            if (dto.Items == null || !dto.Items.Any())
            {
                result.Errors.Add("Đơn hàng phải có ít nhất 1 sản phẩm");
                result.IsValid = false;
            }

            if (dto.DeliveryAddresses == null || !dto.DeliveryAddresses.Any())
            {
                result.Errors.Add("Phải có ít nhất 1 địa chỉ giao hàng");
                result.IsValid = false;
            }

            // Validate B2B requires userId
            if (dto.OrderType == OrderTypeDto.B2B && string.IsNullOrEmpty(dto.UserId))
            {
                result.Errors.Add("Đơn hàng B2B yêu cầu đăng nhập");
                result.IsValid = false;
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Tra cứu đơn hàng (cho Guest)
        /// </summary>
        public async Task<OrderTrackingResult?> TrackOrderAsync(string orderCode, string email)
        {
            // TODO: Query from database
            // For now, return a mock result
            _logger.LogInformation($"Tracking order {orderCode} for {email}");
            
            // In real implementation, query database
            return await Task.FromResult<OrderTrackingResult?>(null);
        }

        #region Helper Methods

        private string GenerateOrderCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"SHT{timestamp}{random}";
        }

        private Models.OrderItem MapOrderItem(OrderItemRequestDto dto)
        {
            return new Models.OrderItem
            {
                ProductId = ObjectId.Parse(dto.ProductId),
                ProductName = "",
                ProductType = dto.ProductType,
                Quantity = dto.Quantity,
                UnitPrice = 0,
                TotalPrice = 0,
                CustomBasketId = string.IsNullOrEmpty(dto.CustomBasketId) ? null : ObjectId.Parse(dto.CustomBasketId)
            };
        }

        private DeliveryAddress MapDeliveryAddress(DeliveryAddressRequestDto dto)
        {
            return new DeliveryAddress
            {
                RecipientName = dto.RecipientName,
                RecipientPhone = dto.RecipientPhone,
                AddressLine = dto.AddressLine,
                Ward = dto.Ward,
                District = dto.District,
                City = dto.City,
                Notes = dto.Notes,
                GreetingMessage = dto.GreetingMessage,
                HideInvoice = dto.HideInvoice
            };
        }

        private decimal CalculateSubTotal(List<OrderItemRequestDto> items)
        {
            // TODO: Query actual prices from database
            return 0;
        }

        private decimal CalculateShippingFee(List<DeliveryAddressRequestDto> addresses)
        {
            if (addresses == null || !addresses.Any())
                return 0;

            // Simple logic: 30k per address
            return addresses.Count * 30000;
        }

        #endregion
    }
}
