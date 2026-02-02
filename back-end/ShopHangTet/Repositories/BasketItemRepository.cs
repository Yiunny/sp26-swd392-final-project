using MongoDB.Bson;
using MongoDB.Driver;
using ShopHangTet.Models;
using ShopHangTet.Interfaces;

namespace ShopHangTet.Repositories;

public class BasketItemRepository : IBasketItemRepository
{
    private readonly IMongoCollection<BasketItem> _collection;
    private readonly IMongoClient _client;

    public BasketItemRepository(IMongoClient client, IMongoDatabase database)
    {
        _client = client;
        _collection = database.GetCollection<BasketItem>("BasketItems");
    }

    public async Task<BasketItem?> GetByIdAsync(string id)
    {
        var filter = Builders<BasketItem>.Filter.Eq(x => x.Id, ObjectId.Parse(id));
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<BasketItem>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<IEnumerable<BasketItem>> GetByCategoryAsync(string category)
    {
        var filter = Builders<BasketItem>.Filter.Eq(x => x.Category, category);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<IEnumerable<BasketItem>> GetByMeaningAsync(string meaning)
    {
        var filter = Builders<BasketItem>.Filter.AnyEq(x => x.Meanings, meaning);
        return await _collection.Find(filter).ToListAsync();
    }

    public async Task<BasketItem> CreateAsync(BasketItem item)
    {
        await _collection.InsertOneAsync(item);
        return item;
    }

    public async Task<bool> UpdateAsync(string id, BasketItem item)
    {
        var filter = Builders<BasketItem>.Filter.Eq(x => x.Id, ObjectId.Parse(id));
        item.UpdatedAt = DateTime.UtcNow;
        var result = await _collection.ReplaceOneAsync(filter, item);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var filter = Builders<BasketItem>.Filter.Eq(x => x.Id, ObjectId.Parse(id));
        var result = await _collection.DeleteOneAsync(filter);
        return result.DeletedCount > 0;
    }

    /// <summary>
    /// ✅ FIX: Atomic stock update với proper validation
    /// </summary>
    public async Task<bool> UpdateStockAsync(string productId, int quantityChange)
    {
        var filter = Builders<BasketItem>.Filter.Eq(x => x.Id, ObjectId.Parse(productId));
        
        // ⚠️ QUAN TRỌNG: Nếu trừ kho, kiểm tra stock >= quantity cần trừ
        if (quantityChange < 0)
        {
            filter &= Builders<BasketItem>.Filter.Gte(x => x.StockQuantity, Math.Abs(quantityChange));
        }
        
        var update = Builders<BasketItem>.Update
            .Inc(x => x.StockQuantity, quantityChange)
            .Set(x => x.UpdatedAt, DateTime.UtcNow);
        
        var result = await _collection.UpdateOneAsync(filter, update);
        
        if (result.ModifiedCount == 0)
        {
            Console.WriteLine($"⚠️ [Stock Update Failed] Product {productId}: insufficient stock or not found");
            return false;
        }
        
        Console.WriteLine($"✅ [Stock Updated] Product {productId}: {quantityChange:+#;-#;0}");
        return true;
    }

    /// <summary>
    /// ✅ FIX: Batch update với TRANSACTION + SESSION
    /// Trước đây: có transaction nhưng không truyền session vào UpdateOneAsync
    /// </summary>
    public async Task<bool> UpdateStockBatchAsync(Dictionary<string, int> productQuantities)
    {
        // ✅ FIX: Sử dụng session cho transaction
        using var session = await _client.StartSessionAsync();
        
        try
        {
            session.StartTransaction();
            
            foreach (var (productId, quantityChange) in productQuantities)
            {
                var filter = Builders<BasketItem>.Filter.Eq(x => x.Id, ObjectId.Parse(productId));
                
                // Validation: không cho trừ nếu stock không đủ
                if (quantityChange < 0)
                {
                    filter &= Builders<BasketItem>.Filter.Gte(x => x.StockQuantity, Math.Abs(quantityChange));
                }
                
                var update = Builders<BasketItem>.Update
                    .Inc(x => x.StockQuantity, quantityChange)
                    .Set(x => x.UpdatedAt, DateTime.UtcNow);
                
                // ✅ FIX: Truyền session vào UpdateOneAsync
                var result = await _collection.UpdateOneAsync(session, filter, update);
                
                if (result.ModifiedCount == 0)
                {
                    Console.WriteLine($"❌ [Batch Stock Update] Failed at product {productId}. Rolling back...");
                    await session.AbortTransactionAsync();
                    return false;
                }
            }
            
            await session.CommitTransactionAsync();
            Console.WriteLine($"✅ [Batch Stock Update] Success: {productQuantities.Count} products updated");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Batch Stock Update] Exception: {ex.Message}");
            await session.AbortTransactionAsync();
            return false;
        }
    }
}
