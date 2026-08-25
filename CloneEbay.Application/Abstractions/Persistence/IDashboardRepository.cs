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

    /// <summary>
    /// Raw, period-scoped fulfilment figures (week/month, as selected on the dashboard) —
    /// distinct from the persisted 12-month SellerPerformance level, which
    /// <see cref="Dashboard.DashboardService"/> layers on top separately.
    /// </summary>
    Task<(int TotalOrders, decimal LateShipmentRate, decimal DisputeRate, decimal PositiveFeedbackRate)> GetPerformanceAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>Distinct buyers in the period, split into one-time vs repeat (>1 order) — spec C.1 "Sales Report".</summary>
    Task<(int TotalBuyers, int OneTimeBuyers, int RepeatBuyers)> GetBuyerInsightsAsync(
        int sellerId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}