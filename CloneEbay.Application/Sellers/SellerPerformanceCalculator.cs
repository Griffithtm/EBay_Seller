using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Sellers;

/// <summary>
/// Dependency-free scoring rules (spec A.2 / A.4). Kept separate from
/// <see cref="SellerPerformanceService"/> so the thresholds can be unit-tested
/// without a database, clock, or DI container.
/// </summary>
public static class SellerPerformanceCalculator
{
    /// <summary>DetermineSellerLevel(defectRate, lateShipRate, trackingRate, casesClosedRate) — spec A.2.</summary>
    public static SellerPerformanceLevel DetermineLevel(
        decimal defectRate, decimal lateShipRate, decimal trackingRate, decimal casesClosedRate)
    {
        if (defectRate <= 0.5m && lateShipRate <= 2m && trackingRate >= 95m && casesClosedRate < 0.3m)
            return SellerPerformanceLevel.TopRated;
        if (defectRate <= 2m && lateShipRate <= 4m && trackingRate >= 90m && casesClosedRate < 1m)
            return SellerPerformanceLevel.AboveStandard;
        return SellerPerformanceLevel.BelowStandard;
    }

    /// <summary>ApplyMinimumRequirements(level, transactions12mo, sales12mo) — spec A.2, TopRated volume gate.</summary>
    public static SellerPerformanceLevel ApplyMinimumRequirements(
        SellerPerformanceLevel level, int transactionsLast12Months, decimal salesLast12Months)
    {
        if (level != SellerPerformanceLevel.TopRated) return level;

        var meetsOption1 = transactionsLast12Months >= 100 && salesLast12Months >= 1000m;
        var meetsOption2 = transactionsLast12Months >= 400;
        return meetsOption1 || meetsOption2 ? level : SellerPerformanceLevel.AboveStandard;
    }

    /// <summary>Full pipeline: raw metrics in, final (post-minimum-requirements) level out.</summary>
    public static SellerPerformanceLevel Evaluate(
        decimal defectRate, decimal lateShipRate, decimal trackingRate, decimal casesClosedRate,
        int transactionsLast12Months, decimal salesLast12Months)
    {
        var level = DetermineLevel(defectRate, lateShipRate, trackingRate, casesClosedRate);
        return ApplyMinimumRequirements(level, transactionsLast12Months, salesLast12Months);
    }

    private const string Critical = "Critical";
    private const string Warning = "Warning";

    /// <summary>Early-warning bands approaching the BelowStandard floor — spec A.4 table.</summary>
    public static IReadOnlyList<SellerPerformanceWarningItem> GetWarnings(
        decimal defectRate, decimal lateShipRate, decimal trackingRate, decimal casesClosedRate)
    {
        var items = new List<SellerPerformanceWarningItem>();

        // Defect rate: Warning 1.5% <= x < 1.8%, Critical 1.8% <= x < 2.0%.
        Add(items, "TransactionDefectRate", defectRate, 2.0m,
            defectRate >= 1.8m && defectRate < 2.0m,
            defectRate >= 1.5m && defectRate < 1.8m,
            "Tỷ lệ giao dịch lỗi đang tăng cao.",
            "Giảm huỷ đơn, xử lý tranh chấp nhanh và đúng quy trình hơn.");

        // Late shipment rate: Warning 3.0% <= x < 3.6%, Critical 3.6% <= x < 4.0%.
        Add(items, "LateShipmentRate", lateShipRate, 4.0m,
            lateShipRate >= 3.6m && lateShipRate < 4.0m,
            lateShipRate >= 3.0m && lateShipRate < 3.6m,
            "Tỷ lệ giao hàng trễ đang tiến gần ngưỡng tối thiểu.",
            "Chuẩn bị hàng và bàn giao vận chuyển sớm hơn so với cam kết.");

        // Tracking rate (higher is better, floor 90%): Warning 91% < x <= 92%, Critical 90% < x <= 91%.
        Add(items, "TrackingUploadedOnTimeRate", trackingRate, 90.0m,
            trackingRate > 90.0m && trackingRate <= 91.0m,
            trackingRate > 91.0m && trackingRate <= 92.0m,
            "Tỷ lệ nhập mã vận đơn đúng hạn đang giảm dần về ngưỡng tối thiểu.",
            "Nhập mã tracking hợp lệ ngay khi bàn giao đơn vị vận chuyển.");

        // Cases closed without seller resolution: Warning 0.75% <= x < 0.9%, Critical 0.9% <= x < 1.0%.
        Add(items, "CasesClosedWithoutSellerResolutionRate", casesClosedRate, 1.0m,
            casesClosedRate >= 0.9m && casesClosedRate < 1.0m,
            casesClosedRate >= 0.75m && casesClosedRate < 0.9m,
            "Tỷ lệ tranh chấp bị đóng mà seller không phải bên giải quyết đang tăng.",
            "Phản hồi và chủ động đề xuất giải quyết tranh chấp sớm hơn.");

        return items;
    }

    public static string? OverallSeverity(IReadOnlyList<SellerPerformanceWarningItem> items)
    {
        if (items.Any(i => i.Severity == Critical)) return Critical;
        if (items.Any(i => i.Severity == Warning)) return Warning;
        return null;
    }

    private static void Add(
        List<SellerPerformanceWarningItem> items,
        string metric,
        decimal value,
        decimal belowStandardThreshold,
        bool isCritical,
        bool isWarning,
        string message,
        string recommendedAction)
    {
        if (isCritical)
            items.Add(new SellerPerformanceWarningItem(metric, value, belowStandardThreshold, Critical, message, recommendedAction));
        else if (isWarning)
            items.Add(new SellerPerformanceWarningItem(metric, value, belowStandardThreshold, Warning, message, recommendedAction));
    }
}
