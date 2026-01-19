using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;
using ShopHangTet.Models;

public class ShopHangTetDbContext : DbContext
{
    public ShopHangTetDbContext(DbContextOptions<ShopHangTetDbContext> options)
        : base(options)
    {
    }

    // Khai báo các Collection trong MongoDB
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Chỉ định tên Collection trong MongoDB
        modelBuilder.Entity<Product>().ToCollection("products");
        modelBuilder.Entity<Order>().ToCollection("orders");
        modelBuilder.Entity<User>().ToCollection("users");
    }
}