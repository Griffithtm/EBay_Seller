namespace CloneEbay.Application.Sellers;

public sealed record SellerPerformanceResponse(
    int SellerId,
    string Level,
    decimal TransactionDefectRate,
    decimal LateShipmentRate,
    decimal TrackingUploadedOnTimeRate,
    decimal CasesClosedWithoutSellerResolutionRate,
    int TransactionsLast12Months,
    decimal SalesLast12Months,
    int MaxActiveListings,
    decimal MaxTotalActiveValue,
    int CurrentActiveListingCount,
    decimal CurrentActiveListingValue,
    DateTime? LastEvaluatedAtUtc,
    DateTime NextEvaluationDateUtc,
    DateTime? ManualOverrideAtUtc,
    int? ManualOverrideByAdminId);

public sealed record SetSellerPerformanceLevelRequest(string Level);

public sealed record SellerPerformanceWarningItem(
    string Metric,
    decimal CurrentValue,
    decimal Threshold,
    string Severity,
    string Message,
    string RecommendedAction);

public sealed record SellerPerformanceWarningResponse(
    string? Severity,
    IReadOnlyList<SellerPerformanceWarningItem> Items,
    DateTime NextEvaluationDateUtc);
