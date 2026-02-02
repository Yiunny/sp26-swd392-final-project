using ShopHangTet.Models;

namespace ShopHangTet.Interfaces;

public interface IBasketTemplateRepository
{
    Task<BasketTemplate?> GetByIdAsync(string id);
    Task<IEnumerable<BasketTemplate>> GetAllAsync();
    Task<IEnumerable<BasketTemplate>> GetBySizeAsync(string size);
    Task<IEnumerable<BasketTemplate>> GetAvailableAsync();

    Task<BasketTemplate> CreateAsync(BasketTemplate template);
    Task<bool> UpdateAsync(string id, BasketTemplate template);
    Task<bool> DeleteAsync(string id);

    Task<bool> UpdateStockAsync(string templateId, int quantityChange);
}