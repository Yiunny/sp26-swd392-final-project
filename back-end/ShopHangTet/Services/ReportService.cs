using ClosedXML.Excel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using ShopHangTet.DTOs;
using ShopHangTet.Models;

namespace ShopHangTet.Services;

public class ReportService : IReportService
{
    private readonly ILogger<ReportService> _logger;
    private readonly IMongoCollection<OrderModel> _ordersCol;
    private readonly IMongoCollection<ReportOrderItemDoc> _orderItemsCol;
    private readonly IMongoCollection<ReportGiftBoxDoc> _giftBoxesCol;
    private readonly IMongoCollection<ReportCollectionDoc> _collectionsCol;
    private readonly IMongoCollection<ReportReviewDoc> _reviewsCol;
    private readonly IMongoCollection<BsonDocument> _itemsCol;

    public ReportService(ILogger<ReportService> logger, IMongoDatabase mongoDatabase)
    {
        _logger = logger;
        _ordersCol = mongoDatabase.GetCollection<OrderModel>("Orders");
        _orderItemsCol = mongoDatabase.GetCollection<ReportOrderItemDoc>("OrderItems");
        _giftBoxesCol = mongoDatabase.GetCollection<ReportGiftBoxDoc>("GiftBoxes");
        _collectionsCol = mongoDatabase.GetCollection<ReportCollectionDoc>("Collections");
        _reviewsCol = mongoDatabase.GetCollection<ReportReviewDoc>("Reviews");
        _itemsCol = mongoDatabase.GetCollection<BsonDocument>("Items");
    }

    [BsonIgnoreExtraElements]
    public class ReportOrderItemDoc
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        [BsonRepresentation(BsonType.String)]
        public string? GiftBoxId { get; set; }

        [BsonRepresentation(BsonType.String)]
        public string? CustomBoxId { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ReportGiftBoxDoc
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("collectionId")]
        public string CollectionId { get; set; } = string.Empty;

        [BsonElement("images")]
        public List<string>? Images { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ReportCollectionDoc
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("coverImage")]
        public string? CoverImage { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class ReportReviewDoc
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("giftBoxId")]
        public string GiftBoxId { get; set; } = string.Empty;

        [BsonElement("rating")]
        public int Rating { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = string.Empty;
    }

    public async Task<DashboardReportDTO> GetDashboardAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var recentFrom = now.AddDays(-30);
            var prevFrom = recentFrom.AddDays(-30);

            var allOrders = await GetAllOrdersAsync();
            var recentOrders = allOrders.Where(o => o.CreatedAt >= recentFrom).ToList();
            var prevOrders = allOrders.Where(o => o.CreatedAt >= prevFrom && o.CreatedAt < recentFrom).ToList();

            var recentRevenue = recentOrders.Sum(o => o.TotalAmount);
            var prevRevenue = prevOrders.Sum(o => o.TotalAmount);

            var revenueGrowth = prevRevenue <= 0
                ? (recentRevenue <= 0 ? 0 : 100.0)
                : (double)((recentRevenue - prevRevenue) / prevRevenue * 100);

            var recentOrderCount = recentOrders.Count;
            var prevOrderCount = prevOrders.Count;
            var orderGrowth = prevOrderCount <= 0
                ? (recentOrderCount <= 0 ? 0 : 100.0)
                : (double)(recentOrderCount - prevOrderCount) / prevOrderCount * 100;

            var b2c = recentOrders.Count(o => o.OrderType == OrderType.B2C);
            var b2b = recentOrders.Count(o => o.OrderType == OrderType.B2B);
            var b2cPercent = recentOrders.Count == 0 ? 0.0 : (double)b2c / recentOrders.Count * 100;
            var b2bPercent = recentOrders.Count == 0 ? 0.0 : (double)b2b / recentOrders.Count * 100;

            var today = now.Date;
            var todayOrders = allOrders.Where(o => o.CreatedAt.Date == today).ToList();
            var todayRevenue = todayOrders.Sum(o => o.TotalAmount);
            var todayOrderCount = todayOrders.Count;

            var statusSummary = new ReportStatusSummaryDTO
            {
                PendingPayment = allOrders.Count(o => o.Status == OrderStatus.PAYMENT_CONFIRMING),
                Preparing = allOrders.Count(o => o.Status == OrderStatus.PREPARING),
                Shipping = allOrders.Count(o => o.Status == OrderStatus.SHIPPING),
                Completed = allOrders.Count(o => o.Status == OrderStatus.COMPLETED),
                Cancelled = allOrders.Count(o => o.Status == OrderStatus.CANCELLED),
                DeliveryFailed = allOrders.Count(o => o.Status == OrderStatus.DELIVERY_FAILED)
            };

            return new DashboardReportDTO
            {
                TotalRevenue = recentRevenue,
                RevenueGrowthPercent = Math.Round(revenueGrowth, 2),
                TotalOrders = recentOrderCount,
                OrderGrowthPercent = Math.Round(orderGrowth, 2),
                TodayRevenue = todayRevenue,
                TodayOrders = todayOrderCount,
                B2CPercent = Math.Round(b2cPercent, 2),
                B2BPercent = Math.Round(b2bPercent, 2),
                StatusSummary = statusSummary
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReportService.GetDashboardAsync failed");
            return new DashboardReportDTO();
        }
    }

    public async Task<RevenueReportDTO> GetRevenueAsync(DateTime? fromDate, DateTime? toDate, string view, string? orderType)
    {
        try
        {
            var start = fromDate ?? DateTime.UtcNow.AddMonths(-1);
            var end = (toDate ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);

            var allOrders = await GetAllOrdersAsync();
            IEnumerable<OrderModel> filteredOrders = allOrders;

            if (!string.IsNullOrWhiteSpace(orderType) && Enum.TryParse<OrderType>(orderType, true, out var ot))
            {
                filteredOrders = filteredOrders.Where(o => o.OrderType == ot);
            }

            var orders = filteredOrders.Where(o => o.CreatedAt >= start && o.CreatedAt <= end).ToList();
            var totalRevenue = orders.Sum(o => o.TotalAmount);

            var prevStart = start.AddYears(-1);
            var prevEnd = end.AddYears(-1);
            var prevOrders = filteredOrders.Where(o => o.CreatedAt >= prevStart && o.CreatedAt <= prevEnd).ToList();
            var prevRevenue = prevOrders.Sum(o => o.TotalAmount);
            var growth = prevRevenue <= 0
                ? (totalRevenue <= 0 ? 0 : 100.0)
                : (double)((totalRevenue - prevRevenue) / prevRevenue * 100);

            var chart = new List<RevenueReportChartItemDTO>();

            if (string.Equals(view, "month", StringComparison.OrdinalIgnoreCase))
            {
                var grouped = orders
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Revenue = g.Sum(o => o.TotalAmount) })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                foreach (var g in grouped)
                {
                    var lastYearRevenue = prevOrders
                        .Where(o => o.CreatedAt.Year == g.Year - 1 && o.CreatedAt.Month == g.Month)
                        .Sum(o => o.TotalAmount);

                    chart.Add(new RevenueReportChartItemDTO
                    {
                        Date = $"{g.Year}-{g.Month:D2}",
                        Revenue = g.Revenue,
                        LastYearRevenue = lastYearRevenue
                    });
                }
            }
            else
            {
                var grouped = orders
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                    .OrderBy(x => x.Date)
                    .ToList();

                foreach (var g in grouped)
                {
                    var lastYearDate = g.Date.AddYears(-1);
                    var lastYearRevenue = prevOrders
                        .Where(o => o.CreatedAt.Date == lastYearDate)
                        .Sum(o => o.TotalAmount);

                    chart.Add(new RevenueReportChartItemDTO
                    {
                        Date = g.Date.ToString("yyyy-MM-dd"),
                        Revenue = g.Revenue,
                        LastYearRevenue = lastYearRevenue
                    });
                }
            }

            var best = chart.OrderByDescending(c => c.Revenue).FirstOrDefault();
            var b2cPercent = orders.Count == 0 ? 0.0 : (double)orders.Count(o => o.OrderType == OrderType.B2C) / orders.Count * 100;
            var b2bPercent = orders.Count == 0 ? 0.0 : (double)orders.Count(o => o.OrderType == OrderType.B2B) / orders.Count * 100;

            return new RevenueReportDTO
            {
                TotalRevenue = totalRevenue,
                GrowthPercent = Math.Round(growth, 2),
                BestDayDate = best?.Date ?? string.Empty,
                BestDayRevenue = best?.Revenue ?? 0m,
                B2CPercent = Math.Round(b2cPercent, 2),
                B2BPercent = Math.Round(b2bPercent, 2),
                Chart = chart
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReportService.GetRevenueAsync failed");
            return new RevenueReportDTO();
        }
    }

    public async Task<List<CollectionPerformanceItemDTO>> GetCollectionsPerformanceAsync()
    {
        var orderItems = await GetAllOrderItemsAsync();
        var giftBoxes = await _giftBoxesCol.Find(Builders<ReportGiftBoxDoc>.Filter.Empty).ToListAsync();
        var collections = await _collectionsCol.Find(Builders<ReportCollectionDoc>.Filter.Empty).ToListAsync();

        var collectionStats = new Dictionary<string, (int Orders, decimal Revenue)>();

        foreach (var item in orderItems)
        {
            if (string.IsNullOrWhiteSpace(item.GiftBoxId))
            {
                continue;
            }

            var giftBox = giftBoxes.FirstOrDefault(g => g.Id == item.GiftBoxId);
            if (giftBox == null || string.IsNullOrWhiteSpace(giftBox.CollectionId))
            {
                continue;
            }

            var collectionId = giftBox.CollectionId;
            if (!collectionStats.TryGetValue(collectionId, out var current))
            {
                current = (0, 0m);
            }

            collectionStats[collectionId] = (current.Orders + 1, current.Revenue + item.TotalPrice);
        }

        var totalRevenue = collectionStats.Values.Sum(x => x.Revenue);

        var result = collectionStats
            .Select(kv =>
            {
                var collection = collections.FirstOrDefault(c => c.Id == kv.Key);
                return new CollectionPerformanceItemDTO
                {
                    CollectionId = kv.Key,
                    CollectionName = collection?.Name ?? string.Empty,
                    Orders = kv.Value.Orders,
                    Revenue = kv.Value.Revenue,
                    Percent = totalRevenue == 0 ? 0 : (double)(kv.Value.Revenue / totalRevenue * 100),
                    Thumbnail = collection?.CoverImage
                };
            })
            .OrderByDescending(x => x.Revenue)
            .Select((x, index) =>
            {
                x.Rank = index + 1;
                return x;
            })
            .ToList();

        return result;
    }

    public async Task<List<GiftBoxPerformanceItemDTO>> GetGiftBoxPerformanceAsync()
    {
        var orderItems = await GetAllOrderItemsAsync();
        var giftBoxes = await _giftBoxesCol.Find(Builders<ReportGiftBoxDoc>.Filter.Empty).ToListAsync();

        List<ReportReviewDoc> reviews;
        try
        {
            reviews = await _reviewsCol.Find(Builders<ReportReviewDoc>.Filter.Eq(r => r.Status, "APPROVED")).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Reviews from MongoDB.");
            reviews = new List<ReportReviewDoc>();
        }

        var stats = new Dictionary<string, (string Name, string? Image, int Sold, decimal Revenue, List<int> Ratings)>();
        foreach (var giftBox in giftBoxes)
        {
            stats[giftBox.Id] = (giftBox.Name, giftBox.Images?.FirstOrDefault(), 0, 0m, new List<int>());
        }

        foreach (var item in orderItems)
        {
            if (string.IsNullOrWhiteSpace(item.GiftBoxId) || !stats.ContainsKey(item.GiftBoxId))
            {
                continue;
            }

            var current = stats[item.GiftBoxId];
            current.Sold += item.Quantity;
            current.Revenue += item.TotalPrice;
            stats[item.GiftBoxId] = current;
        }

        foreach (var review in reviews)
        {
            if (!string.IsNullOrWhiteSpace(review.GiftBoxId) && stats.ContainsKey(review.GiftBoxId))
            {
                stats[review.GiftBoxId].Ratings.Add(review.Rating);
            }
        }

        return stats
            .Select(kv => new GiftBoxPerformanceItemDTO
            {
                GiftBoxId = kv.Key,
                GiftBoxName = kv.Value.Name,
                SoldQuantity = kv.Value.Sold,
                Revenue = kv.Value.Revenue,
                AvgRating = kv.Value.Ratings.Count == 0 ? 0.0 : Math.Round(kv.Value.Ratings.Average(), 2),
                Image = kv.Value.Image,
                TopProduct = null,
                MarketingSuggestions = null
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();
    }

    public async Task<B2cB2bComparisonDTO> GetB2cB2bComparisonAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var oneYearAgo = now.AddYears(-1);
            var allOrders = await GetAllOrdersAsync();
            var orders = allOrders.Where(o => o.CreatedAt >= oneYearAgo).ToList();

            var b2cOrders = orders.Where(o => o.OrderType == OrderType.B2C).ToList();
            var b2bOrders = orders.Where(o => o.OrderType == OrderType.B2B).ToList();

            var b2cRevenue = b2cOrders.Sum(o => o.TotalAmount);
            var b2bRevenue = b2bOrders.Sum(o => o.TotalAmount);
            var b2cAvg = b2cOrders.Count == 0 ? 0m : b2cRevenue / b2cOrders.Count;

            var orderItems = await GetAllOrderItemsAsync();
            var totalGiftBoxes = orderItems.Where(i => !string.IsNullOrWhiteSpace(i.GiftBoxId)).Sum(i => i.Quantity);

            var monthly = new List<B2cB2bMonthlyDTO>();
            var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-11);

            for (var i = 0; i < 12; i++)
            {
                var monthStart = startMonth.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1);
                var monthOrders = orders.Where(o => o.CreatedAt >= monthStart && o.CreatedAt < monthEnd).ToList();

                monthly.Add(new B2cB2bMonthlyDTO
                {
                    Month = monthStart.ToString("yyyy-MM"),
                    B2COrders = monthOrders.Count(o => o.OrderType == OrderType.B2C),
                    B2BOrders = monthOrders.Count(o => o.OrderType == OrderType.B2B)
                });
            }

            return new B2cB2bComparisonDTO
            {
                B2CRevenue = b2cRevenue,
                B2COrders = b2cOrders.Count,
                B2CAvgOrderValue = Math.Round(b2cAvg, 2),
                B2BRevenue = b2bRevenue,
                B2BOrders = b2bOrders.Count,
                TotalGiftBoxes = totalGiftBoxes,
                MonthlyOrdersChart = monthly
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReportService.GetB2cB2bComparisonAsync failed");
            return new B2cB2bComparisonDTO();
        }
    }

    public async Task<List<InventoryAlertItemDTO>> GetInventoryAlertAsync(int threshold)
    {
        try
        {
            var filter = Builders<BsonDocument>.Filter.Lte("stockQuantity", threshold);
            var items = await _itemsCol.Find(filter).ToListAsync();

            return items.Select(i => new InventoryAlertItemDTO
            {
                ItemId = i.GetValue("_id", BsonNull.Value).ToString() ?? string.Empty,
                ItemName = i.GetValue("name", string.Empty).AsString,
                Stock = i.GetValue("stockQuantity", 0).AsInt32,
                Threshold = threshold
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Items for inventory alert.");
            return new List<InventoryAlertItemDTO>();
        }
    }

    public async Task<byte[]> ExportRevenueAsync(DateTime? fromDate, DateTime? toDate, string view, string? orderType)
    {
        var dto = await GetRevenueAsync(fromDate, toDate, view, orderType);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Revenue");
        ws.Cell(1, 1).Value = "Total Revenue";
        ws.Cell(1, 2).Value = dto.TotalRevenue;
        ws.Cell(2, 1).Value = "GrowthPercent";
        ws.Cell(2, 2).Value = dto.GrowthPercent;
        ws.Cell(4, 1).Value = "Date";
        ws.Cell(4, 2).Value = "Revenue";
        ws.Cell(4, 3).Value = "LastYearRevenue";

        var row = 5;
        foreach (var item in dto.Chart)
        {
            ws.Cell(row, 1).Value = item.Date;
            ws.Cell(row, 2).Value = item.Revenue;
            ws.Cell(row, 3).Value = item.LastYearRevenue;
            row++;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportCollectionsAsync()
    {
        var list = await GetCollectionsPerformanceAsync();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Collections");
        ws.Cell(1, 1).Value = "Rank";
        ws.Cell(1, 2).Value = "CollectionId";
        ws.Cell(1, 3).Value = "CollectionName";
        ws.Cell(1, 4).Value = "Orders";
        ws.Cell(1, 5).Value = "Revenue";
        ws.Cell(1, 6).Value = "%";

        var row = 2;
        foreach (var item in list)
        {
            ws.Cell(row, 1).Value = item.Rank;
            ws.Cell(row, 2).Value = item.CollectionId;
            ws.Cell(row, 3).Value = item.CollectionName;
            ws.Cell(row, 4).Value = item.Orders;
            ws.Cell(row, 5).Value = item.Revenue;
            ws.Cell(row, 6).Value = item.Percent;
            row++;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportGiftBoxesAsync()
    {
        var list = await GetGiftBoxPerformanceAsync();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("GiftBoxes");
        ws.Cell(1, 1).Value = "GiftBoxId";
        ws.Cell(1, 2).Value = "GiftBoxName";
        ws.Cell(1, 3).Value = "SoldQuantity";
        ws.Cell(1, 4).Value = "Revenue";
        ws.Cell(1, 5).Value = "AvgRating";

        var row = 2;
        foreach (var item in list)
        {
            ws.Cell(row, 1).Value = item.GiftBoxId;
            ws.Cell(row, 2).Value = item.GiftBoxName;
            ws.Cell(row, 3).Value = item.SoldQuantity;
            ws.Cell(row, 4).Value = item.Revenue;
            ws.Cell(row, 5).Value = item.AvgRating;
            row++;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportB2cB2bAsync()
    {
        var dto = await GetB2cB2bComparisonAsync();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("B2C_B2B");

        ws.Cell(1, 1).Value = "B2CRevenue";
        ws.Cell(1, 2).Value = dto.B2CRevenue;
        ws.Cell(2, 1).Value = "B2COrders";
        ws.Cell(2, 2).Value = dto.B2COrders;
        ws.Cell(3, 1).Value = "B2CAvgOrderValue";
        ws.Cell(3, 2).Value = dto.B2CAvgOrderValue;

        ws.Cell(5, 1).Value = "Month";
        ws.Cell(5, 2).Value = "B2COrders";
        ws.Cell(5, 3).Value = "B2BOrders";

        var row = 6;
        foreach (var item in dto.MonthlyOrdersChart)
        {
            ws.Cell(row, 1).Value = item.Month;
            ws.Cell(row, 2).Value = item.B2COrders;
            ws.Cell(row, 3).Value = item.B2BOrders;
            row++;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> ExportInventoryAlertAsync(int threshold)
    {
        var list = await GetInventoryAlertAsync(threshold);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("InventoryAlert");
        ws.Cell(1, 1).Value = "ItemId";
        ws.Cell(1, 2).Value = "ItemName";
        ws.Cell(1, 3).Value = "Stock";
        ws.Cell(1, 4).Value = "Threshold";

        var row = 2;
        foreach (var item in list)
        {
            ws.Cell(row, 1).Value = item.ItemId;
            ws.Cell(row, 2).Value = item.ItemName;
            ws.Cell(row, 3).Value = item.Stock;
            ws.Cell(row, 4).Value = item.Threshold;
            row++;
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private async Task<List<OrderModel>> GetAllOrdersAsync()
    {
        try
        {
            return await _ordersCol.Find(Builders<OrderModel>.Filter.Empty).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Orders from MongoDB.");
            return new List<OrderModel>();
        }
    }

    private async Task<List<ReportOrderItemDoc>> GetAllOrderItemsAsync()
    {
        try
        {
            return await _orderItemsCol.Find(Builders<ReportOrderItemDoc>.Filter.Empty).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load OrderItems from MongoDB.");
            return new List<ReportOrderItemDoc>();
        }
    }
}
