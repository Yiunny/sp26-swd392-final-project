using Microsoft.EntityFrameworkCore;
using ShopHangTet.Data;
using ShopHangTet.DTOs;
using ShopHangTet.Models;
using System;

namespace ShopHangTet.Services
{
    public class CartService : ICartService
    {
        private readonly ShopHangTetDbContext _context;

        public CartService(ShopHangTetDbContext context)
        {
            _context = context;
        }

        // Thêm lệnh Include để gộp bảng
        private async Task<Cart> GetOrCreateCartAsync(string? userId, string? sessionId)
        {
            var cart = await _context.Set<Cart>()
                .Include(c => c.Items) // <--- CHÌA KHÓA MA THUẬT NẰM Ở ĐÂY
                .FirstOrDefaultAsync(c =>
                    (!string.IsNullOrEmpty(userId) && c.UserId == userId) ||
                    (!string.IsNullOrEmpty(sessionId) && c.SessionId == sessionId));

            if (cart == null)
            {
                cart = new Cart { UserId = userId, SessionId = sessionId };
                _context.Set<Cart>().Add(cart);
            }
            return cart;
        }

        // Truy vấn DB lấy Tên thật xuất ra JSON
        private async Task<CartDto> MapToDtoAsync(Cart cart)
        {
            var dto = new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                SessionId = cart.SessionId,
                TotalAmount = cart.Items.Sum(i => i.Quantity * i.UnitPrice),
                TotalItems = cart.Items.Sum(i => i.Quantity),
                Items = new List<CartItemDto>()
            };

            foreach (var item in cart.Items)
            {
                string name = "Sản phẩm không xác định";

                // Chạy qua kho GiftBoxes lấy tên thật
                if (item.Type == OrderItemType.READY_MADE && !string.IsNullOrEmpty(item.GiftBoxId))
                {
                    var giftBox = await _context.Set<GiftBox>().FirstOrDefaultAsync(g => g.Id == item.GiftBoxId);
                    if (giftBox != null) name = giftBox.Name;
                }
                else if (item.Type == OrderItemType.MIX_MATCH)
                {
                    name = "Hộp quà tự chọn (Mix & Match)";
                }

                dto.Items.Add(new CartItemDto
                {
                    Id = item.Id,
                    Type = item.Type,
                    GiftBoxId = item.GiftBoxId,
                    CustomBoxId = item.CustomBoxId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Name = name // Tên đã rực rỡ xuất hiện!
                });
            }
            return dto;
        }

        public async Task<ApiResponse<CartDto>> GetCartAsync(string? userId, string? sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            // Dùng await vì hàm MapToDtoAsync giờ phải chạy đi tìm tên Sản phẩm
            return ApiResponse<CartDto>.SuccessResult(await MapToDtoAsync(cart));
        }

        public async Task<ApiResponse<CartDto>> AddToCartAsync(string? userId, string? sessionId, AddToCartDto dto)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);

            //Bắt buộc lấy giá từ Database phòng hờ ai đó "hack" API
            decimal unitPrice = 0;
            if (dto.Type == OrderItemType.READY_MADE && !string.IsNullOrEmpty(dto.GiftBoxId))
            {
                var giftBox = await _context.Set<GiftBox>().FirstOrDefaultAsync(g => g.Id == dto.GiftBoxId);
                if (giftBox == null) return ApiResponse<CartDto>.ErrorResult("Không tìm thấy hộp quà này trong hệ thống!");

                unitPrice = giftBox.Price; // Lấy giá chính hãng
            }

            // Kiểm tra xem món đó đã có trong giỏ chưa
            var existingItem = cart.Items.FirstOrDefault(i =>
                i.Type == dto.Type &&
                ((dto.Type == OrderItemType.READY_MADE && i.GiftBoxId == dto.GiftBoxId) ||
                 (dto.Type == OrderItemType.MIX_MATCH && i.CustomBoxId == dto.CustomBoxId)));

            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
                existingItem.UnitPrice = unitPrice; // Cập nhật lại giá nhỡ shop đổi giá
            }
            else
            {
                var newItem = new CartItem
                {
                    CartId = cart.Id,             
                    SessionId = cart.SessionId,
                    Type = dto.Type,
                    GiftBoxId = dto.GiftBoxId,
                    CustomBoxId = dto.CustomBoxId,
                    Quantity = dto.Quantity,
                    UnitPrice = unitPrice
                };
                cart.Items.Add(newItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<CartDto>.SuccessResult(await MapToDtoAsync(cart), "Đã thêm vào giỏ hàng");
        }

        // Cập nhật số lượng
        public async Task<ApiResponse<CartDto>> UpdateCartItemAsync(string? userId, string? sessionId, string cartItemId, UpdateCartItemDto dto)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (item == null) return ApiResponse<CartDto>.ErrorResult("Không tìm thấy sản phẩm trong giỏ");

            item.Quantity = dto.Quantity;
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<CartDto>.SuccessResult(await MapToDtoAsync(cart), "Đã cập nhật số lượng");
        }

        // Xóa 1 món
        public async Task<ApiResponse<bool>> RemoveFromCartItemAsync(string? userId, string? sessionId, string cartItemId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (item != null)
            {
                cart.Items.Remove(item);
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return ApiResponse<bool>.SuccessResult(true, "Đã xóa sản phẩm khỏi giỏ");
        }

        // Xóa sạch giỏ
        public async Task<ApiResponse<bool>> ClearCartAsync(string? userId, string? sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            cart.Items.Clear();
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResult(true, "Đã làm sạch giỏ hàng");
        }
    }
}