using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class SellerPerformanceRepository(CloneEbayDbContext dbContext) : ISellerPerformanceRepository
{
    private const int HandlingTimeDays = SellerHandlingTimePolicy.HandlingTimeDays;

    public Task<SellerPerformance?> GetBySellerIdAsync(int sellerId, CancellationToken cancellationToken = default) =>
        dbContext.SellerPerformances.SingleOrDefaultAsync(p => p.SellerId == sellerId, cancellationToken);

    public async Task<SellerPerformance> GetOrCreateAsync(int sellerId, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.SellerPerformances.SingleOrDefaultAsync(p => p.SellerId == sellerId, cancellationToken);
        if (existing is not null) return existing;

        var created = new SellerPerformance
        {
            SellerId = sellerId,
            Level = SellerPerformanceLevel.BelowStandard,
            NextEvaluationDateUtc = SellerPerformanceEvaluationSchedule.NextEvaluationDate(nowUtc)
        };
        await dbContext.SellerPerformances.AddAsync(created, cancellationToken);
        return created;
    }

    public async Task<SellerMetricsSnapshot> ComputeMetricsAsync(
        int sellerId,
        DateTime periodStartUtc,
        DateTime nowUtc,
        CancellationToken cancellationToken = default)
    {
        var sellerOrderIds = dbContext.OrderItems
            .Where(item => item.Product.SellerId == sellerId)
            .Select(item => item.OrderId)
            .Distinct();

        var orders = await dbContext.Orders
            .AsNoTracking()
            .Where(order => sellerOrderIds.Contains(order.Id) && order.OrderDateUtc >= periodStartUtc && order.OrderDateUtc <= nowUtc)
            .Select(order => new
            {
                order.Id,
                order.Status,
                order.OrderDateUtc,
                order.TotalPrice,
                ShippedAtUtc = order.ShippingInfo != null ? order.ShippingInfo.ShippedAtUtc : null,
                TrackingNumber = order.ShippingInfo != null ? order.ShippingInfo.TrackingNumber : null
            })
            .ToListAsync(cancellationToken);

        var orderIds = orders.Select(o => o.Id).ToArray();

        var disputeStats = await dbContext.Disputes
            .AsNoTracking()
            .Where(d => orderIds.Contains(d.OrderId))
            .Select(d => new { d.OrderId, d.Status, d.Resolution })
            .ToListAsync(cancellationToken);

        var defectiveOrderIds = new HashSet<int>(
            orders.Where(o => string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)).Select(o => o.Id));
        defectiveOrderIds.UnionWith(disputeStats.Select(d => d.OrderId));

        var shipped = orders.Where(o => o.ShippedAtUtc.HasValue).ToArray();
        var lateShipped = shipped.Count(o => (o.ShippedAtUtc!.Value - o.OrderDateUtc).TotalDays > HandlingTimeDays);

        // Orders past "WaitingPayment" are ones the seller was obligated to fulfil.
        var fulfillable = orders.Where(o => !string.Equals(o.Status, "WaitingPayment", StringComparison.OrdinalIgnoreCase)).ToArray();
        var onTimeTracking = fulfillable.Count(o =>
            o.ShippedAtUtc.HasValue &&
            !string.IsNullOrWhiteSpace(o.TrackingNumber) &&
            (o.ShippedAtUtc.Value - o.OrderDateUtc).TotalDays <= HandlingTimeDays);

        var closedDisputes = disputeStats.Where(d => string.Equals(d.Status, DisputeStatus.Resolved, StringComparison.OrdinalIgnoreCase)).ToArray();
        var closedWithoutResolution = closedDisputes.Count(d => string.IsNullOrWhiteSpace(d.Resolution));

        var salesAmount = orders
            .Where(o => !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            .Sum(o => o.TotalPrice);

        return new SellerMetricsSnapshot(
            TotalOrders: orders.Count,
            DefectiveOrders: defectiveOrderIds.Count,
            ShippedOrders: shipped.Length,
            LateShippedOrders: lateShipped,
            FulfillableOrders: fulfillable.Length,
            OnTimeTrackingOrders: onTimeTracking,
            ClosedDisputes: closedDisputes.Length,
            ClosedDisputesWithoutSellerResolution: closedWithoutResolution,
            SalesAmount: salesAmount);
    }

    public async Task<(int ActiveListingCount, decimal ActiveListingTotalValue)> GetActiveListingStatsAsync(
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var listings = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.SellerId == sellerId && (p.Status == ProductStatus.Active || p.Status == ProductStatus.OutOfStock))
            .Select(p => new { p.IsAuction, p.Price, Quantity = p.Inventory.Quantity })
            .ToListAsync(cancellationToken);

        var totalValue = listings.Sum(p => p.IsAuction ? p.Price : p.Price * p.Quantity);
        return (listings.Count, totalValue);
    }

    public async Task<IReadOnlyList<int>> GetAllSellerIdsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Stores
            .AsNoTracking()
            .Where(store => store.VerificationStatus == VerificationStatus.Approved)
            .Select(store => store.SellerId)
            .ToListAsync(cancellationToken);
}
