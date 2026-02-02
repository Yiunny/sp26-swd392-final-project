using ShopHangTet.Models;

namespace ShopHangTet.Interfaces;

public interface IBasketItemRepository
{
    Task<BasketItem?> GetByIdAsync(string id);
    Task<IEnumerable<BasketItem>> GetAllAsync();
    Task<IEnumerable<BasketItem>> GetByCategoryAsync(string category);
    Task<IEnumerable<BasketItem>> GetByMeaningAsync(string meaning);

    Task<BasketItem> CreateAsync(BasketItem item);
    Task<bool> UpdateAsync(string id, BasketItem item);
    Task<bool> DeleteAsync(string id);

    Task<bool> UpdateStockAsync(string productId, int quantityChange);
    Task<bool> UpdateStockBatchAsync(Dictionary<string, int> productQuantities);
}