using ShopHangTet.Models;

public static class SeedData
{
    public static void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ShopHangTetDbContext>();

        // Kiểm tra nếu chưa có sản phẩm nào thì mới thêm
        if (!context.Products.Any())
        {
            context.Products.AddRange(
                new Product { Name = "Rượu Vang Đỏ", Price = 500000, Category = "Rượu", Meaning = "Quà biếu đối tác", IsComponent = true },
                new Product { Name = "Bánh Quy Bơ", Price = 150000, Category = "Bánh", Meaning = "Quà sum vầy", IsComponent = true },
                new Product { Name = "Giỏ quà Đoàn Viên", Price = 1200000, Category = "Giỏ quà sẵn", Meaning = "Quà sum vầy", IsComponent = false }
            );
            context.SaveChanges();
            Console.WriteLine("----> Đã Seed dữ liệu mẫu thành công!");
        }
    }
}