using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Storage;
using ShopHangTet.Models;

namespace ShopHangTet.Data
{
    public class ShopHangTetDbContext : DbContext
{
    public ShopHangTetDbContext(DbContextOptions<ShopHangTetDbContext> options)
        : base(options)
    {
        this.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
    }

    // User & Authentication
    public DbSet<UserModel> Users { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<OtpRecord> OtpRecords { get; set; }

    // Products & Collections
    public DbSet<Collection> Collections { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<GiftBox> GiftBoxes { get; set; }
    public DbSet<Item> Items { get; set; }
    public DbSet<CustomBox> CustomBoxes { get; set; }

    // Orders & Cart
    public DbSet<Cart> Carts { get; set; }
    public DbSet<OrderModel> Orders { get; set; }
    public DbSet<OrderDelivery> OrderDeliveries { get; set; }
    public DbSet<InventoryLog> InventoryLogs { get; set; }

    // Support & Reviews
    public DbSet<Review> Reviews { get; set; }
    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }

    // Legacy models for compatibility
    public DbSet<BasketItem> BasketItems { get; set; }
    public DbSet<BasketTemplate> BasketTemplates { get; set; }
    public DbSet<DeliverySlot> DeliverySlots { get; set; }
    public DbSet<Product> ProductsLegacy { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Main entities
        modelBuilder.Entity<UserModel>();
        modelBuilder.Entity<Address>();
        modelBuilder.Entity<OtpRecord>();
        
        modelBuilder.Entity<Collection>();
        modelBuilder.Entity<Tag>();
        modelBuilder.Entity<GiftBox>();
        modelBuilder.Entity<Item>();
        modelBuilder.Entity<CustomBox>();
        
        modelBuilder.Entity<Cart>();
        modelBuilder.Entity<OrderModel>();
        modelBuilder.Entity<OrderDelivery>();
        modelBuilder.Entity<InventoryLog>();
        
        modelBuilder.Entity<Review>();
        modelBuilder.Entity<ChatSession>();
        modelBuilder.Entity<SystemConfig>();
        
        // Legacy entities for compatibility
        modelBuilder.Entity<BasketItem>();
        modelBuilder.Entity<BasketTemplate>();
        modelBuilder.Entity<CustomBasket>();
        modelBuilder.Entity<DeliverySlot>();
        modelBuilder.Entity<Product>();
    }
}
} // namespace ShopHangTet.Data