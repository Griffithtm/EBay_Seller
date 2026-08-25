using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

/// <summary>
/// Raw counts backing the trailing-12-month seller performance metrics (spec A.2).
/// The service layer turns these into percentages so the arithmetic (and the
/// zero-denominator guards) stays testable without a database.
/// </summary>
public sealed record SellerMetricsSnapshot(
    int TotalOrders,
    int DefectiveOrders,
    int ShippedOrders,
    int LateShippedOrders,
    int FulfillableOrders,
    int OnTimeTrackingOrders,
    int ClosedDisputes,
    int ClosedDisputesWithoutSellerResolution,
    decimal SalesAmount);

public interface ISellerPerformanceRepository
{
    Task<SellerPerformance?> GetBySellerIdAsync(int sellerId, CancellationToken cancellationToken = default);

    /// <summary>Fetches the tracked row, creating a default BelowStandard one if the seller has never been evaluated.</summary>
    Task<SellerPerformance> GetOrCreateAsync(int sellerId, DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<SellerMetricsSnapshot> ComputeMetricsAsync(
        int sellerId,
        DateTime periodStartUtc,
        DateTime nowUtc,
        CancellationToken cancellationToken = default);

    Task<(int ActiveListingCount, decimal ActiveListingTotalValue)> GetActiveListingStatsAsync(
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetAllSellerIdsAsync(CancellationToken cancellationToken = default);
}
