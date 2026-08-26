namespace CloneEbay.Application.Dashboard;

public sealed record DashboardSummaryDto(
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageOrderValue,
    string Period,
    DateTime FromUtc,
    DateTime ToUtc);

public sealed record TopProductDto(
    int ProductId,
    string Title,
    string? ImageUrl,
    int QuantitySold,
    decimal Revenue,
    int RemainingStock,
    bool IsAuction);

public sealed record RevenueChartPointDto(
    string Label,
    string Date,
    decimal Revenue,
    int OrderCount);

public sealed record RevenueChartDto(
    string Period,
    decimal TotalRevenue,
    int TotalOrders,
    IReadOnlyList<RevenueChartPointDto> Points);

public sealed record SellerPerformanceDto(
    string Period,
    int TotalOrders,
    decimal LateShipmentRate,
    decimal DisputeRate,
    string SellerLevel,
    decimal PositiveFeedbackRate);

public sealed record DailyOrderAggregateDto(
    DateTime DateUtc,
    decimal Revenue,
    int OrderCount);