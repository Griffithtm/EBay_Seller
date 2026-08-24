using CloneEbay.Application.Dashboard;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface IDashboardRepository
{
    Task<(decimal TotalRevenue, int TotalOrders, decimal AverageOrderValue)> GetSummaryAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyOrderAggregateDto>> GetDailyAggregatesAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<SellerPerformanceDto> GetPerformanceAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}