namespace CloneEbay.Application.Performance;

public sealed record PerformanceSummaryDto(
    string Period,
    decimal TotalRevenue,
    int TotalOrders,
    decimal SellingCosts,
    string SellingCostsNote,
    bool TrafficAvailable,
    string SellerLevel,
    string SellerLevelTitle,
    string SellerLevelMessage);

public sealed record PerformanceSalesReportDto(
    string Period,
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageOrderValue,
    int TotalBuyers,
    int OneTimeBuyers,
    int RepeatBuyers);

public sealed record PerformanceTrafficDto(bool Available, string Note);

public sealed record PerformanceServiceMetricsDto(
    decimal TransactionDefectRate,
    decimal LateShipmentRate,
    decimal TrackingUploadedOnTimeRate,
    decimal CasesClosedWithoutSellerResolutionRate,
    string Note);
