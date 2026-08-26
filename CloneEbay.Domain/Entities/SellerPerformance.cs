using CloneEbay.Domain.Common;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Domain.Entities;

/// <summary>
/// One row per seller. Snapshot of the last computed performance metrics plus the
/// resulting <see cref="SellerPerformanceLevel"/>. Recomputed by the monthly
/// evaluation job (day 20 of each month); read on every product-creation request
/// to enforce <see cref="SellingLimitPolicy"/>.
/// </summary>
public sealed class SellerPerformance : BaseEntity
{
    public int SellerId { get; set; }
    public SellerPerformanceLevel Level { get; set; } = SellerPerformanceLevel.BelowStandard;

    // Trailing 12-month metrics, expressed as percentages (0-100), snapshot at last evaluation.
    public decimal TransactionDefectRate { get; set; }
    public decimal LateShipmentRate { get; set; }
    public decimal TrackingUploadedOnTimeRate { get; set; }
    public decimal CasesClosedWithoutSellerResolutionRate { get; set; }

    // Volume figures backing the TopRated minimum-requirements gate (A.2).
    public int TransactionsLast12Months { get; set; }
    public decimal SalesLast12Months { get; set; }

    public DateTime? LastEvaluatedAtUtc { get; set; }
    public DateTime NextEvaluationDateUtc { get; set; }

    /// <summary>
    /// Admin exception-handling override (e.g. a seller disputes a wrongly-computed
    /// level). Not part of the original spec's automated scoring — an explicit,
    /// audited escape hatch on top of it. The next automatic evaluation (monthly job)
    /// recomputes <see cref="Level"/> from real metrics and clears these fields.
    /// </summary>
    public DateTime? ManualOverrideAtUtc { get; set; }
    public int? ManualOverrideByAdminId { get; set; }

    public User Seller { get; set; } = null!;
}

/// <summary>
/// Shared seller-fulfilment constants. The schema has no per-seller handling-time
/// SLA field, so a single fixed commitment is used everywhere "late shipment" /
/// "tracking on time" needs to be judged (both the 12-month performance evaluation
/// and the shorter-period Dashboard performance widget).
/// </summary>
public static class SellerHandlingTimePolicy
{
    public const int HandlingTimeDays = 3;
}

/// <summary>
/// Pure scheduling rule for the monthly re-evaluation job (spec A.3): evaluation
/// happens on day 20 of each month. Kept dependency-free so it is unit-testable
/// without a clock/database.
/// </summary>
public static class SellerPerformanceEvaluationSchedule
{
    public const int EvaluationDay = 20;

    public static DateTime NextEvaluationDate(DateTime nowUtc) =>
        nowUtc.Day >= EvaluationDay
            ? new DateTime(nowUtc.Year, nowUtc.Month, EvaluationDay, 0, 0, 0, DateTimeKind.Utc).AddMonths(1)
            : new DateTime(nowUtc.Year, nowUtc.Month, EvaluationDay, 0, 0, 0, DateTimeKind.Utc);

    public static bool IsEvaluationDue(DateTime nowUtc) => nowUtc.Day == EvaluationDay;
}

/// <summary>
/// Selling-limit policy derived from <see cref="SellerPerformanceLevel"/> (spec A.5).
/// Kept as a pure, dependency-free static class so it is trivially unit-testable,
/// mirroring the existing <see cref="ReturnRequestStatus"/> transition-table pattern.
/// </summary>
/// <remarks>
/// The spec states these value caps in USD ($500 / $5,000 / $50,000), but every
/// price in this codebase (<see cref="Product.Price"/>, <see cref="Order.TotalPrice"/>)
/// is stored in VND. Applying the raw USD numbers here would block a seller from
/// listing even a single realistically-priced item. The caps below apply the spec's
/// USD figures scaled by an approximate 25,000 VND/USD rate, so the policy still
/// meaningfully limits sellers instead of blocking everyone outright.
/// </remarks>
public static class SellingLimitPolicy
{
    private const decimal UsdToVndRate = 25_000m;

    public static (int MaxActiveListings, decimal MaxTotalActiveValue) GetLimits(SellerPerformanceLevel level) => level switch
    {
        SellerPerformanceLevel.BelowStandard => (10, 500m * UsdToVndRate),
        SellerPerformanceLevel.AboveStandard => (100, 5000m * UsdToVndRate),
        SellerPerformanceLevel.TopRated => (1000, 50000m * UsdToVndRate),
        _ => (10, 500m * UsdToVndRate)
    };

    /// <summary>
    /// Both conditions must hold (AND). Strict less-than: touching the limit exactly
    /// still counts as "no room left" (spec A.5).
    /// </summary>
    public static bool CanList(
        SellerPerformanceLevel level,
        int currentActiveListingCount,
        decimal currentActiveTotalValue,
        decimal newListingEstimatedValue)
    {
        var (maxCount, maxValue) = GetLimits(level);
        return currentActiveListingCount < maxCount
            && currentActiveTotalValue + newListingEstimatedValue < maxValue;
    }
}
