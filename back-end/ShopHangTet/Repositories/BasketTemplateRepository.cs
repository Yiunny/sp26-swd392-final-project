using MongoDB.Bson;
using MongoDB.Driver;
using ShopHangTet.Models;
using ShopHangTet.Interfaces;

namespace ShopHangTet.Repositories;

public class BasketTemplateRepository : IBasketTemplateRepository
{
    private readonly IMongoCollection<BasketTemplate> _collection;

    public BasketTemplateRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<BasketTemplate>("BasketTemplates");
    }

    public async Task<BasketTemplate?> GetByIdAsync(string id)
    {
        var filter = Builders<BasketTemplate>.Filter.Eq(x => x.Id, ObjectId.Parse(id));
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<BasketTemplate>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<IEnumerable<BasketTemplate>> GetBySizeAsync(string size)
    {
        var filter = Builders<BasketTemplate>.Filter.Eq(x => x.Size, size);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<BasketTemplate>> GetAvailableAsync()
    {
        var filter = Builders<BasketTemplate>.Filter.Gt(x => x.StockQuantity, 0);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<BasketTemplate> CreateAsync(BasketTemplate template)
    {
        await _collection.InsertOneAsync(template);
        return template;
    }

    public async Task<bool> UpdateAsync(string id, BasketTemplate template)
    {
        var filter = Builders<BasketTemplate>.Filter.Eq(x => x.Id, ObjectId.Parse(id));
        template.UpdatedAt = DateTime.UtcNow;
        var result = await _collection.ReplaceOneAsync(filter, template);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var filter = Builders<BasketTemplate>.Filter.Eq(x => x.Id, ObjectId.Parse(id));
        var result = await _collection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    /// <summary>
    /// Atomic stock update cho template (giống BasketItem)
    /// </summary>
    public async Task<bool> UpdateStockAsync(string templateId, int quantityChange)
    {
        var filter = Builders<BasketTemplate>.Filter.Eq(x => x.Id, ObjectId.Parse(templateId));
        
        if (quantityChange < 0)
        {
            filter &= Builders<BasketTemplate>.Filter.Gte(x => x.StockQuantity, Math.Abs(quantityChange));
        }
        
        var update = Builders<BasketTemplate>.Update
            .Inc(x => x.StockQuantity, quantityChange)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        
        if (result.ModifiedCount == 0)
        {
            Console.WriteLine($"⚠️ [Template Stock Update Failed] Template {templateId}");
            return false;
        }
        
        Console.WriteLine($"✅ [Template Stock Updated] Template {templateId}: {quantityChange:+#;-#;0}");
        return true;
    }
}
