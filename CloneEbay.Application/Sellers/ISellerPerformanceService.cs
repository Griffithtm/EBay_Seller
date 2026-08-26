namespace CloneEbay.Application.Sellers;

public interface ISellerPerformanceService
{
    Task<SellerPerformanceResponse> GetAsync(int sellerId, CancellationToken cancellationToken = default);

    Task<SellerPerformanceWarningResponse> GetEarlyWarningAsync(int sellerId, CancellationToken cancellationToken = default);

    /// <summary>Recomputes metrics + level for one seller from live order/dispute data and persists the result.</summary>
    Task EvaluateSellerAsync(int sellerId, CancellationToken cancellationToken = default);

    /// <summary>Entry point for the monthly background job: evaluates every verified seller whose evaluation is due.</summary>
    Task<int> EvaluateDueSellersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces the A.5 selling-limit gate for one more listing of the given estimated
    /// value. Throws <see cref="Common.AppProblemException"/> (403) when the seller has
    /// no room left. Lazily creates a BelowStandard performance row on first use.
    /// </summary>
    Task EnsureCanListAsync(int sellerId, decimal newListingEstimatedValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin-only manual override (exception handling, not part of the automatic
    /// scoring algorithm) — see remarks on CloneEbay.Domain.Entities.SellerPerformance.ManualOverrideAtUtc.
    /// The next monthly evaluation recomputes the level from real metrics and clears the override.
    /// </summary>
    Task<SellerPerformanceResponse> OverrideLevelAsync(int sellerId, string level, int adminUserId, CancellationToken cancellationToken = default);
}
