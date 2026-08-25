using System.Text.Json;
using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Dashboard;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class DashboardRepository(CloneEbayDbContext dbContext) : IDashboardRepository
{
    private static readonly string[] ExcludedStatuses = ["Cancelled", "Failed"];

    public async Task<(decimal TotalRevenue, int TotalOrders, decimal AverageOrderValue)> GetSummaryAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var itemsQuery = BaseSellerOrderItems(sellerId, fromUtc, toUtc);

        var totalRevenue = await itemsQuery
            .SumAsync(item => (decimal?)(item.Quantity * item.UnitPrice), cancellationToken) ?? 0m;

        var totalOrders = await itemsQuery
            .Select(item => item.OrderId)
            .Distinct()
            .CountAsync(cancellationToken);

        var aov = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 2) : 0m;
        return (totalRevenue, totalOrders, aov);
    }

    public async Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var itemsQuery = BaseSellerOrderItems(sellerId, fromUtc, toUtc);

        var grouped = await itemsQuery
            .GroupBy(item => new
            {
                item.ProductId,
                item.Product.Title,
                item.Product.ImagesJson,
                item.Product.IsAuction,
                Stock = (int?)item.Product.Inventory!.Quantity ?? 0
            })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Title,
                g.Key.ImagesJson,
                g.Key.IsAuction,
                g.Key.Stock,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.Revenue)
            .ThenByDescending(x => x.QuantitySold)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return grouped.Select(g => new TopProductDto(
            g.ProductId,
            g.Title,
            ExtractFirstImage(g.ImagesJson),
            g.QuantitySold,
            g.Revenue,
            g.Stock,
            g.IsAuction
        )).ToArray();
    }

    public async Task<IReadOnlyList<DailyOrderAggregateDto>> GetDailyAggregatesAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var raw = await BaseSellerOrderItems(sellerId, fromUtc, toUtc)
            .Select(item => new
            {
                item.OrderId,
                Date = item.Order.OrderDateUtc.Date,
                Amount = item.Quantity * item.UnitPrice
            })
            .ToListAsync(cancellationToken);

        return raw
            .GroupBy(x => x.Date)
            .Select(g => new DailyOrderAggregateDto(
                DateTime.SpecifyKind(g.Key, DateTimeKind.Utc),
                g.Sum(x => x.Amount),
                g.Select(x => x.OrderId).Distinct().Count()
            ))
            .OrderBy(x => x.DateUtc)
            .ToArray();
    }

    public async Task<(int TotalOrders, decimal LateShipmentRate, decimal DisputeRate, decimal PositiveFeedbackRate)> GetPerformanceAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        var orders = await BaseSellerOrderItems(sellerId, fromUtc, toUtc)
            .Select(item => item.OrderId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            var feedbackForEmptyPeriod = await dbContext.Feedbacks
                .AsNoTracking()
                .Where(f => f.SellerId == sellerId)
                .Select(f => (decimal?)f.PositiveRate)
                .SingleOrDefaultAsync(cancellationToken);
            return (0, 0m, 0m, feedbackForEmptyPeriod ?? 100m);
        }

        var shipmentInfo = await dbContext.Orders
            .AsNoTracking()
            .Where(o => orders.Contains(o.Id))
            .Select(o => new
            {
                o.OrderDateUtc,
                ShippedAtUtc = o.ShippingInfo != null ? o.ShippingInfo.ShippedAtUtc : null
            })
            .ToListAsync(cancellationToken);

        var shipped = shipmentInfo.Where(o => o.ShippedAtUtc.HasValue).ToArray();
        var lateShipped = shipped.Count(o => (o.ShippedAtUtc!.Value - o.OrderDateUtc).TotalDays > SellerHandlingTimePolicy.HandlingTimeDays);
        var lateShipmentRate = shipped.Length == 0 ? 0m : Math.Round(lateShipped * 100m / shipped.Length, 2);

        var disputeCount = await dbContext.Disputes
            .AsNoTracking()
            .Where(d => orders.Contains(d.OrderId))
            .Select(d => d.OrderId)
            .Distinct()
            .CountAsync(cancellationToken);
        var disputeRate = Math.Round(disputeCount * 100m / orders.Count, 2);

        var positiveFeedbackRate = await dbContext.Feedbacks
            .AsNoTracking()
            .Where(f => f.SellerId == sellerId)
            .Select(f => (decimal?)f.PositiveRate)
            .SingleOrDefaultAsync(cancellationToken) ?? 100m;

        return (orders.Count, lateShipmentRate, disputeRate, positiveFeedbackRate);
    }

    private IQueryable<OrderItem> BaseSellerOrderItems(int sellerId, DateTime fromUtc, DateTime toUtc) =>
        dbContext.OrderItems
            .AsNoTracking()
            .Include(item => item.Product)
                .ThenInclude(p => p.Inventory)
            .Include(item => item.Order)
            .Where(item => item.Product.SellerId == sellerId)
            .Where(item => item.Order.OrderDateUtc >= fromUtc && item.Order.OrderDateUtc < toUtc)
            .Where(item => !ExcludedStatuses.Contains(item.Order.Status));

    private static string? ExtractFirstImage(string? imagesJson)
    {
        if (string.IsNullOrWhiteSpace(imagesJson)) return null;
        try
        {
            var list = JsonSerializer.Deserialize<string[]>(imagesJson);
            return list?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}