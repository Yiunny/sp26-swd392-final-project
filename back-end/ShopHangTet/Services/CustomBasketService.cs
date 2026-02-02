using ShopHangTet.Interfaces;
using ShopHangTet.Models;
using ShopHangTet.DTOs;
using MongoDB.Bson;

namespace ShopHangTet.Services
{
    /// <summary>
    /// CustomBasketService - Xử lý Mix & Match
    /// </summary>
    public class CustomBasketService : ICustomBasketService
    {
        private readonly IBasketItemRepository _itemRepo;
        private readonly IBasketTemplateRepository _templateRepo;
        private readonly ILogger<CustomBasketService> _logger;

        public CustomBasketService(
            IBasketItemRepository itemRepo,
            IBasketTemplateRepository templateRepo,
            ILogger<CustomBasketService> logger)
        {
            _itemRepo = itemRepo;
            _templateRepo = templateRepo;
            _logger = logger;
        }

        /// <summary>
        /// Tạo custom basket với validation Mix & Match rules
        /// </summary>
        public async Task<CustomBasket> CreateCustomBasketAsync(CreateCustomBasketDto dto)
        {
            _logger.LogInformation($"Creating custom basket with {dto.Items.Count} items");

            // Validate Mix & Match rules
            var validation = await ValidateMixMatchRulesAsync(dto.Items);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(string.Join(", ", validation.Errors));
            }

            // Calculate total price
            var totalPrice = await CalculatePriceAsync(dto.Items);

            // Create basket
            var basket = new CustomBasket
            {
                Id = ObjectId.GenerateNewId(),
                Items = dto.Items.Select(i => new BasketItemSelection
                {
                    ItemId = ObjectId.Parse(i.ItemId),
                    ItemName = i.Name ?? "",
                    Quantity = i.Quantity,
                    UnitPrice = i.Price ?? 0
                }).ToList(),
                TotalPrice = totalPrice,
                GreetingMessage = dto.GreetingMessage ?? "",
                CanvaCardLink = dto.CanvaCardLink ?? "",
                HideInvoice = dto.HideInvoice,
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(dto.BasketTemplateId))
            {
                basket.BasketTemplateId = ObjectId.Parse(dto.BasketTemplateId);
            }

            _logger.LogInformation($"Custom basket created: {basket.Id}");
            return basket;
        }

        /// <summary>
        /// Validate Mix & Match rules:
        /// - Ít nhất 1 đồ uống
        /// - 2-4 món ăn
        /// - Tối đa 1 rượu
        /// </summary>
        public async Task<MixMatchValidationResult> ValidateMixMatchRulesAsync(List<BasketItemDto> items)
        {
            var result = new MixMatchValidationResult { IsValid = true };

            if (items == null || !items.Any())
            {
                result.IsValid = false;
                result.Errors.Add("Phải có ít nhất 1 item trong giỏ quà");
                return result;
            }

            // Count items by category
            var drinkCount = items.Where(i => i.Category?.ToUpper() == "DRINK").Sum(i => i.Quantity);
            var foodCount = items.Where(i => i.Category?.ToUpper() == "FOOD" || i.Category?.ToUpper() == "NUT").Sum(i => i.Quantity);
            var alcoholCount = items.Where(i => i.IsAlcohol || i.Category?.ToUpper() == "ALCOHOL").Sum(i => i.Quantity);

            // Rule 1: Ít nhất 1 đồ uống
            if (drinkCount < 1)
            {
                result.IsValid = false;
                result.Errors.Add("Giỏ quà phải có ít nhất 1 đồ uống");
            }

            // Rule 2: 2-4 món ăn
            if (foodCount < 2)
            {
                result.IsValid = false;
                result.Errors.Add("Giỏ quà phải có ít nhất 2 món ăn");
            }

            if (foodCount > 4)
            {
                result.IsValid = false;
                result.Errors.Add("Giỏ quà tối đa 4 món ăn");
            }

            // Rule 3: Tối đa 1 rượu
            if (alcoholCount > 1)
            {
                result.IsValid = false;
                result.Errors.Add("Giỏ quà tối đa 1 sản phẩm có cồn");
            }

            return await Task.FromResult(result);
        }

        /// <summary>
        /// Tính giá giỏ quà Mix & Match
        /// </summary>
        public async Task<decimal> CalculatePriceAsync(List<BasketItemDto> items)
        {
            decimal total = 0;

            foreach (var item in items)
            {
                if (item.Price.HasValue)
                {
                    total += item.Price.Value * item.Quantity;
                }
                else
                {
                    // Query price from database
                    var product = await _itemRepo.GetByIdAsync(item.ItemId);
                    if (product != null)
                    {
                        total += product.Price * item.Quantity;
                    }
                }
            }

            return total;
        }
    }
}
