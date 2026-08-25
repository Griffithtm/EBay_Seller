using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Sellers;

/// <summary>
/// Implements spec Part A (Seller Performance Level). Metrics are computed over a
/// rolling 12-month window ending "now" (not a fixed calendar year) from live
/// Order/Dispute/ShippingInfo data — see <see cref="ISellerPerformanceRepository.ComputeMetricsAsync"/>
/// for the exact mapping onto this project's schema (documented there, since the
/// schema has no explicit per-seller handling-time SLA field to read from).
/// </summary>
public sealed class SellerPerformanceService(
    ISellerPerformanceRepository repository,
    IUnitOfWork unitOfWork,
    IClock clock) : ISellerPerformanceService
{
    public async Task<SellerPerformanceResponse> GetAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        var performance = await repository.GetOrCreateAsync(sellerId, clock.UtcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var (activeCount, activeValue) = await repository.GetActiveListingStatsAsync(sellerId, cancellationToken);
        return Map(performance, activeCount, activeValue);
    }

    public async Task<SellerPerformanceWarningResponse> GetEarlyWarningAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        var performance = await repository.GetOrCreateAsync(sellerId, clock.UtcNow, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var warnings = SellerPerformanceCalculator.GetWarnings(
            performance.TransactionDefectRate,
            performance.LateShipmentRate,
            performance.TrackingUploadedOnTimeRate,
            performance.CasesClosedWithoutSellerResolutionRate);
        return new SellerPerformanceWarningResponse(
            SellerPerformanceCalculator.OverallSeverity(warnings),
            warnings,
            performance.NextEvaluationDateUtc);
    }

    public async Task EvaluateSellerAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var periodStart = now.AddMonths(-12);
        var metrics = await repository.ComputeMetricsAsync(sellerId, periodStart, now, cancellationToken);

        var defectRate = Rate(metrics.DefectiveOrders, metrics.TotalOrders);
        var lateShipmentRate = Rate(metrics.LateShippedOrders, metrics.ShippedOrders);
        // No fulfillable orders yet (brand-new seller) => benefit of the doubt, not a fault.
        var trackingRate = metrics.FulfillableOrders == 0 ? 100m : Rate(metrics.OnTimeTrackingOrders, metrics.FulfillableOrders);
        var casesClosedRate = Rate(metrics.ClosedDisputesWithoutSellerResolution, metrics.ClosedDisputes);

        var level = SellerPerformanceCalculator.Evaluate(
            defectRate, lateShipmentRate, trackingRate, casesClosedRate,
            metrics.TotalOrders, metrics.SalesAmount);

        var performance = await repository.GetOrCreateAsync(sellerId, now, cancellationToken);

        performance.TransactionDefectRate = defectRate;
        performance.LateShipmentRate = lateShipmentRate;
        performance.TrackingUploadedOnTimeRate = trackingRate;
        performance.CasesClosedWithoutSellerResolutionRate = casesClosedRate;
        performance.TransactionsLast12Months = metrics.TotalOrders;
        performance.SalesLast12Months = metrics.SalesAmount;
        performance.Level = level;
        performance.LastEvaluatedAtUtc = now;
        performance.NextEvaluationDateUtc = SellerPerformanceEvaluationSchedule.NextEvaluationDate(now);
        // A real evaluation supersedes any prior admin exception-handling override.
        performance.ManualOverrideAtUtc = null;
        performance.ManualOverrideByAdminId = null;

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> EvaluateDueSellersAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        if (!SellerPerformanceEvaluationSchedule.IsEvaluationDue(now)) return 0;

        var sellerIds = await repository.GetAllSellerIdsAsync(cancellationToken);
        var evaluated = 0;
        foreach (var sellerId in sellerIds)
        {
            var existing = await repository.GetBySellerIdAsync(sellerId, cancellationToken);
            // Already evaluated for this month's cycle (e.g. host restarted the same day) — skip.
            if (existing?.LastEvaluatedAtUtc is { } lastEvaluated && lastEvaluated.Year == now.Year && lastEvaluated.Month == now.Month)
            {
                continue;
            }

            await EvaluateSellerAsync(sellerId, cancellationToken);
            evaluated++;
        }

        return evaluated;
    }

    public async Task EnsureCanListAsync(int sellerId, decimal newListingEstimatedValue, CancellationToken cancellationToken = default)
    {
        var performance = await repository.GetOrCreateAsync(sellerId, clock.UtcNow, cancellationToken);
        var (activeCount, activeValue) = await repository.GetActiveListingStatsAsync(sellerId, cancellationToken);

        if (SellingLimitPolicy.CanList(performance.Level, activeCount, activeValue, newListingEstimatedValue))
        {
            return;
        }

        var (maxCount, maxValue) = SellingLimitPolicy.GetLimits(performance.Level);
        throw new AppProblemException(
            403,
            "SELLING_LIMIT_EXCEEDED",
            $"Cấp độ tài khoản hiện tại ({performance.Level}) chỉ cho phép tối đa {maxCount} listing đang hoạt động " +
            $"với tổng giá trị dưới {maxValue:0.##}. Bạn đang có {activeCount} listing trị giá {activeValue:0.##}.");
    }

    public async Task<SellerPerformanceResponse> OverrideLevelAsync(int sellerId, string level, int adminUserId, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<SellerPerformanceLevel>(level, ignoreCase: true, out var parsedLevel))
        {
            throw new AppProblemException(422, "INVALID_PERFORMANCE_LEVEL", $"'{level}' is not a valid performance level.");
        }

        var now = clock.UtcNow;
        var performance = await repository.GetOrCreateAsync(sellerId, now, cancellationToken);
        performance.Level = parsedLevel;
        performance.ManualOverrideAtUtc = now;
        performance.ManualOverrideByAdminId = adminUserId;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var (activeCount, activeValue) = await repository.GetActiveListingStatsAsync(sellerId, cancellationToken);
        return Map(performance, activeCount, activeValue);
    }

    private static decimal Rate(int numerator, int denominator) =>
        denominator == 0 ? 0m : Math.Round(numerator * 100m / denominator, 4);

    private static SellerPerformanceResponse Map(SellerPerformance performance, int activeCount, decimal activeValue)
    {
        var (maxCount, maxValue) = SellingLimitPolicy.GetLimits(performance.Level);
        return new SellerPerformanceResponse(
            performance.SellerId,
            performance.Level.ToString(),
            performance.TransactionDefectRate,
            performance.LateShipmentRate,
            performance.TrackingUploadedOnTimeRate,
            performance.CasesClosedWithoutSellerResolutionRate,
            performance.TransactionsLast12Months,
            performance.SalesLast12Months,
            maxCount,
            maxValue,
            activeCount,
            activeValue,
            performance.LastEvaluatedAtUtc,
            performance.NextEvaluationDateUtc,
            performance.ManualOverrideAtUtc,
            performance.ManualOverrideByAdminId);
    }
}
