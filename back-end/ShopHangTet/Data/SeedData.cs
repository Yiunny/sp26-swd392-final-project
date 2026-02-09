using ShopHangTet.Models;
using ShopHangTet.Data;
using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    public static async Task InitializeAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ShopHangTetDbContext>();

        try
        {
            // Đảm bảo database được tạo
            await context.Database.EnsureCreatedAsync();

            // Seed theo thứ tự dependency
            await SeedTagsAsync(context);
            await SeedCollectionsAsync(context);
            await SeedItemsAsync(context);
            await SeedGiftBoxesAsync(context);
            await SeedDeliverySlotsAsync(context);

            await context.SaveChangesAsync();
            Console.WriteLine("----> Seed dữ liệu thành công: Tags, Collections, Items, GiftBoxes, DeliverySlots");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Seed error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private static async Task SeedTagsAsync(ShopHangTetDbContext context)
    {
        if (await context.Tags.AnyAsync()) return;

        var tags = new List<Tag>
        {
            // MEANING Tags
            new Tag { Name = "Tài lộc", Type = "MEANING", IsActive = true },
            new Tag { Name = "Phát đạt", Type = "MEANING", IsActive = true },
            new Tag { Name = "An khang", Type = "MEANING", IsActive = true },
            new Tag { Name = "Thịnh vượng", Type = "MEANING", IsActive = true },
            new Tag { Name = "Sum vầy", Type = "MEANING", IsActive = true },

            // RECIPIENT Tags
            new Tag { Name = "Gia đình", Type = "RECIPIENT", IsActive = true },
            new Tag { Name = "Bạn bè", Type = "RECIPIENT", IsActive = true },
            new Tag { Name = "Đối tác", Type = "RECIPIENT", IsActive = true },
            new Tag { Name = "Nhân viên", Type = "RECIPIENT", IsActive = true },
            new Tag { Name = "Người lớn tuổi", Type = "RECIPIENT", IsActive = true },
            new Tag { Name = "Sếp", Type = "RECIPIENT", IsActive = true },

            // OCCASION Tags
            new Tag { Name = "Tết Nguyên Đán", Type = "OCCASION", IsActive = true },
            new Tag { Name = "Khai trương", Type = "OCCASION", IsActive = true },
            new Tag { Name = "Tri ân", Type = "OCCASION", IsActive = true }
        };

        await context.Tags.AddRangeAsync(tags);
    }

    private static async Task SeedCollectionsAsync(ShopHangTetDbContext context)
    {
        if (await context.Collections.AnyAsync()) return;

        var collections = new List<Collection>
        {
            new Collection
            {
                Name = "Xuân Đoàn Viên",
                Description = "Bộ sưu tập quà Tết truyền thống, ấm áp cho gia đình",
                DisplayOrder = 1,
                IsActive = true
            },
            new Collection
            {
                Name = "Cát Tường Phú Quý", 
                Description = "Bộ sưu tập quà Tết cao cấp, sang trọng",
                DisplayOrder = 2,
                IsActive = true
            },
            new Collection
            {
                Name = "Lộc Xuân Doanh Nghiệp",
                Description = "Bộ sưu tập quà Tết dành cho doanh nghiệp",
                DisplayOrder = 3,
                IsActive = true
            },
            new Collection
            {
                Name = "An Nhiên Tân Xuân",
                Description = "Bộ sưu tập quà Tết sức khỏe, tinh tế",
                DisplayOrder = 4,
                IsActive = true
            },
            new Collection
            {
                Name = "Xuân Gắn Kết",
                Description = "Bộ sưu tập quà Tết nhẹ nhàng, thân tình",
                DisplayOrder = 5,
                IsActive = true
            }
        };

        await context.Collections.AddRangeAsync(collections);
    }

    private static async Task SeedItemsAsync(ShopHangTetDbContext context)
    {
        if (await context.Items.AnyAsync()) return;

        var items = new List<Item>
        {
            // HẠT (6 items)
            new Item { Name = "Hạt điều rang muối", Category = ItemCategory.NUT, Price = 150000, StockQuantity = 1000, IsActive = true },
            new Item { Name = "Hạt macca", Category = ItemCategory.NUT, Price = 200000, StockQuantity = 500, IsActive = true },
            new Item { Name = "Hạt hạnh nhân", Category = ItemCategory.NUT, Price = 180000, StockQuantity = 800, IsActive = true },
            new Item { Name = "Hạt óc chó", Category = ItemCategory.NUT, Price = 220000, StockQuantity = 600, IsActive = true },
            new Item { Name = "Hạt dẻ cười", Category = ItemCategory.NUT, Price = 190000, StockQuantity = 700, IsActive = true },
            new Item { Name = "Đậu phộng rang", Category = ItemCategory.NUT, Price = 80000, StockQuantity = 1500, IsActive = true },

            // BÁNH - KẸO (7 items)
            new Item { Name = "Butter cookies", Category = ItemCategory.FOOD, Price = 80000, StockQuantity = 1500, IsActive = true },
            new Item { Name = "Bánh quy bơ Đan Mạch", Category = ItemCategory.FOOD, Price = 120000, StockQuantity = 1000, IsActive = true },
            new Item { Name = "Socola Jinkeli", Category = ItemCategory.FOOD, Price = 100000, StockQuantity = 900, IsActive = true },
            new Item { Name = "Socola Ferrero", Category = ItemCategory.FOOD, Price = 150000, StockQuantity = 800, IsActive = true },
            new Item { Name = "Kẹo tiramisu", Category = ItemCategory.FOOD, Price = 70000, StockQuantity = 1200, IsActive = true },
            new Item { Name = "Kẹo nougat", Category = ItemCategory.FOOD, Price = 65000, StockQuantity = 1300, IsActive = true },
            new Item { Name = "Bánh pía mini", Category = ItemCategory.FOOD, Price = 90000, StockQuantity = 1100, IsActive = true },

            // MỨT - TRÁI CÂY SẤY (6 items)
            new Item { Name = "Mứt xoài", Category = ItemCategory.FOOD, Price = 110000, StockQuantity = 800, IsActive = true },
            new Item { Name = "Mứt dừa non", Category = ItemCategory.FOOD, Price = 95000, StockQuantity = 900, IsActive = true },
            new Item { Name = "Mứt gừng", Category = ItemCategory.FOOD, Price = 85000, StockQuantity = 700, IsActive = true },
            new Item { Name = "Mứt dứa", Category = ItemCategory.FOOD, Price = 90000, StockQuantity = 850, IsActive = true },
            new Item { Name = "Nho khô", Category = ItemCategory.FOOD, Price = 130000, StockQuantity = 600, IsActive = true },
            new Item { Name = "Táo đỏ sấy", Category = ItemCategory.FOOD, Price = 140000, StockQuantity = 550, IsActive = true },

            // TRÀ (5 items)
            new Item { Name = "Trà ô long", Category = ItemCategory.DRINK, Price = 120000, StockQuantity = 1000, IsActive = true },
            new Item { Name = "Trà sen Tây Hồ", Category = ItemCategory.DRINK, Price = 150000, StockQuantity = 600, IsActive = true },
            new Item { Name = "Trà lài túi lọc", Category = ItemCategory.DRINK, Price = 100000, StockQuantity = 1200, IsActive = true },
            new Item { Name = "Trà thảo mộc", Category = ItemCategory.DRINK, Price = 110000, StockQuantity = 900, IsActive = true },
            new Item { Name = "Trà hoa quả", Category = ItemCategory.DRINK, Price = 105000, StockQuantity = 950, IsActive = true },

            // RƯỢU (4 items)
            new Item { Name = "Rượu vang đỏ", Category = ItemCategory.ALCOHOL, Price = 500000, StockQuantity = 200, IsAlcohol = true, IsActive = true },
            new Item { Name = "Rượu Batise", Category = ItemCategory.ALCOHOL, Price = 350000, StockQuantity = 150, IsAlcohol = true, IsActive = true },
            new Item { Name = "Rượu Chivas 12", Category = ItemCategory.ALCOHOL, Price = 1200000, StockQuantity = 80, IsAlcohol = true, IsActive = true },
            new Item { Name = "Rượu Chivas 21", Category = ItemCategory.ALCOHOL, Price = 2500000, StockQuantity = 40, IsAlcohol = true, IsActive = true },

            // ĐẶC SẢN MẶN (3 items)
            new Item { Name = "Khô gà lá chanh", Category = ItemCategory.FOOD, Price = 180000, StockQuantity = 400, IsActive = true },
            new Item { Name = "Khô bò", Category = ItemCategory.FOOD, Price = 200000, StockQuantity = 350, IsActive = true },
            new Item { Name = "Chà bông cá hồi", Category = ItemCategory.FOOD, Price = 220000, StockQuantity = 300, IsActive = true }
        };

        await context.Items.AddRangeAsync(items);
        Console.WriteLine($"----> Seeded {items.Count} Items");
    }

    private static async Task SeedGiftBoxesAsync(ShopHangTetDbContext context)
    {
        if (await context.GiftBoxes.AnyAsync()) return;

        var collections = await context.Collections.ToListAsync();
        var tags = await context.Tags.ToListAsync();
        var items = await context.Items.ToListAsync();
        
        if (!collections.Any() || !items.Any()) return;

        var xuanDoanVien = collections.FirstOrDefault(c => c.Name == "Xuân Đoàn Viên");
        var catTuong = collections.FirstOrDefault(c => c.Name == "Cát Tường Phú Quý");
        
        if (xuanDoanVien == null || catTuong == null) return;

        // Get tag IDs
        var giaDinhTag = tags.FirstOrDefault(t => t.Name == "Gia đình")?.Id ?? "";
        var truyenThongTag = tags.FirstOrDefault(t => t.Name == "Sum vầy")?.Id ?? "";
        var caoCapTag = tags.FirstOrDefault(t => t.Name == "Thịnh vượng")?.Id ?? "";

        var giftBoxes = new List<GiftBox>
        {
            new GiftBox
            {
                Name = "Xuân Đoàn Viên - Gia Ấm", 
                Description = "Hộp quà gia đình ấm áp với hạt điều, trà lài và bánh cookies",
                Price = 650000,
                CollectionId = xuanDoanVien.Id,
                Tags = new List<string> { giaDinhTag, truyenThongTag },
                Images = new List<string> { "gia-am-1.jpg", "gia-am-2.jpg" },
                Items = new List<GiftBoxItem>
                {
                    new GiftBoxItem { ItemId = items.First(i => i.Name == "Hạt điều rang muối").Id, Quantity = 1 },
                    new GiftBoxItem { ItemId = items.First(i => i.Name == "Mứt dừa non").Id, Quantity = 1 },
                    new GiftBoxItem { ItemId = items.First(i => i.Name == "Butter cookies").Id, Quantity = 1 },
                    new GiftBoxItem { ItemId = items.First(i => i.Name == "Trà lài túi lọc").Id, Quantity = 1 }
                },
                IsActive = true
            },
            new GiftBox
            {
                Name = "Cát Tường Phú Quý - Doanh Gia",
                Description = "Hộp quà cao cấp dành cho đối tác quan trọng",
                Price = 3200000,
                CollectionId = catTuong.Id,
                Tags = new List<string> { caoCapTag },
                Images = new List<string> { "doanh-gia-1.jpg" },
                Items = new List<GiftBoxItem>
                {
                    new GiftBoxItem { ItemId = items.First(i => i.Name == "Rượu Chivas 12").Id, Quantity = 1 },
                    new GiftBoxItem { ItemId = items.First(i => i.Name == "Hạt dẻ cười").Id, Quantity = 1 },
                    new GiftBoxItem { ItemId = items.First(i => i.Name == "Socola Ferrero").Id, Quantity = 1 },
                    new GiftBoxItem { ItemId = items.First(i => i.Name == "Trà ô long").Id, Quantity = 1 }
                },
                IsActive = true
            }
        };

        await context.GiftBoxes.AddRangeAsync(giftBoxes);
        Console.WriteLine($"----> Seeded {giftBoxes.Count} GiftBoxes");
    }

    private static async Task SeedDeliverySlotsAsync(ShopHangTetDbContext context)
    {
        if (await context.DeliverySlots.AnyAsync()) return;

        var slots = new List<DeliverySlot>();
        var startDate = new DateTime(2026, 1, 20); // Gần Tết 2026

        // Tạo slots cho 10 ngày
        for (int day = 0; day < 10; day++)
        {
            var date = startDate.AddDays(day);
            
            // 3 khung giờ mỗi ngày
            slots.Add(new DeliverySlot
            {
                DeliveryDate = date,
                TimeSlot = "8AM-12PM",
                MaxOrdersPerSlot = 50,
                CurrentOrderCount = 0,
                IsLocked = false
            });

            slots.Add(new DeliverySlot
            {
                DeliveryDate = date,
                TimeSlot = "1PM-5PM",
                MaxOrdersPerSlot = 50,
                CurrentOrderCount = 0,
                IsLocked = false
            });

            slots.Add(new DeliverySlot
            {
                DeliveryDate = date,
                TimeSlot = "6PM-9PM",
                MaxOrdersPerSlot = 30, // Tối ít hơn
                CurrentOrderCount = 0,
                IsLocked = false
            });
        }

        await context.DeliverySlots.AddRangeAsync(slots);
        Console.WriteLine($"----> Seeded {slots.Count} DeliverySlots");
    }
}