using ShopHangTet.DTOs;
using ShopHangTet.Models;

namespace ShopHangTet.Services
{
    /// <summary>
    /// Interface cho CustomBasketService - Mix & Match operations
    /// </summary>
    public interface ICustomBasketService
    {
        // === Mix & Match Operations ===
        Task<CustomBasket> CreateCustomBasketAsync(CreateCustomBasketDto dto);
        
        // === Validation ===
        Task<MixMatchValidationResult> ValidateMixMatchRulesAsync(List<BasketItemDto> items);
        
        // === Pricing ===
        Task<decimal> CalculatePriceAsync(List<BasketItemDto> items);
    }

    /// <summary>
    /// Kết quả validation Mix & Match
    /// </summary>
    public class MixMatchValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
