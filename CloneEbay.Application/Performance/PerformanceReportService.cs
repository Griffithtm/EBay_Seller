using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Dashboard;
using CloneEbay.Application.Sellers;

namespace CloneEbay.Application.Performance;

/// <summary>
/// Implements spec Part C.3 endpoints (Summary/Sales/Traffic/ServiceMetrics — the part
/// marked "chưa đạt chuẩn, không copy nguyên"). Two fixes applied from the start, per the
/// spec's own instructions: real <paramref name="sellerId"/> from the authenticated caller
/// (no hardcoded demo GUID), and Sales Report actually reports revenue (the reference
/// project's flagged defect — a report named "Sales" with no sales figures).
/// Part C.2 (seller level + early-warning) already exists in <see cref="ISellerPerformanceService"/>
/// and is reused here rather than duplicated.
/// </summary>
public sealed class PerformanceReportService(
    IDashboardRepository dashboardRepository,
    IPaymentsReportRepository paymentsReportRepository,
    ISellerPerformanceService sellerPerformance,
    IClock clock) : IPerformanceReportService
{
    private const string TrafficNote = "Chưa có hệ thống theo dõi lượt xem/traffic trong project này — không hiển thị số liệu giả.";
    private const string ServiceMetricsNote = "Đây là 4 chỉ số vận hành 12 tháng gần nhất (cùng nguồn với Cấp độ tài khoản), không tính riêng theo kỳ đã chọn.";

    public async Task<PerformanceSummaryDto> GetSummaryAsync(int sellerId, string? period, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = NormalizePeriod(period);
        var (fromUtc, toUtc) = DashboardService.GetDateRange(normalizedPeriod, clock.UtcNow);
        var (totalRevenue, totalOrders, _) = await dashboardRepository.GetSummaryAsync(sellerId, fromUtc, toUtc, cancellationToken);
        var paymentFigures = await paymentsReportRepository.GetFiguresAsync(sellerId, fromUtc, toUtc, cancellationToken);
        var level = await sellerPerformance.GetAsync(sellerId, cancellationToken);

        var (title, message) = level.Level switch
        {
            "TopRated" => ("Top Rated", "Tài khoản của bạn đạt chuẩn cao nhất, hạn mức đăng bán tối đa."),
            "AboveStandard" => ("Above Standard", "Tài khoản đạt chuẩn — duy trì các chỉ số vận hành để đạt Top Rated."),
            _ => ("Below Standard", "Tài khoản đang dưới chuẩn — cải thiện tỷ lệ giao trễ/tranh chấp để nâng cấp độ.")
        };

        return new PerformanceSummaryDto(
            normalizedPeriod, totalRevenue, totalOrders,
            SellingCosts: paymentFigures.Expenses, SellingCostsNote: "Phí dịch vụ sàn (Final Value Fee 10%) tính trên các đơn hoàn tất.",
            TrafficAvailable: false,
            level.Level, title, message);
    }

    public async Task<PerformanceSalesReportDto> GetSalesReportAsync(int sellerId, string? period, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = NormalizePeriod(period);
        var (fromUtc, toUtc) = DashboardService.GetDateRange(normalizedPeriod, clock.UtcNow);
        var (totalRevenue, totalOrders, aov) = await dashboardRepository.GetSummaryAsync(sellerId, fromUtc, toUtc, cancellationToken);
        var (totalBuyers, oneTime, repeat) = await dashboardRepository.GetBuyerInsightsAsync(sellerId, fromUtc, toUtc, cancellationToken);
        return new PerformanceSalesReportDto(normalizedPeriod, totalRevenue, totalOrders, aov, totalBuyers, oneTime, repeat);
    }

    public Task<PerformanceTrafficDto> GetTrafficAsync(int sellerId, string? period, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PerformanceTrafficDto(Available: false, Note: TrafficNote));

    public async Task<PerformanceServiceMetricsDto> GetServiceMetricsAsync(int sellerId, string? period, CancellationToken cancellationToken = default)
    {
        var level = await sellerPerformance.GetAsync(sellerId, cancellationToken);
        return new PerformanceServiceMetricsDto(
            level.TransactionDefectRate, level.LateShipmentRate, level.TrackingUploadedOnTimeRate,
            level.CasesClosedWithoutSellerResolutionRate, ServiceMetricsNote);
    }

    private static string NormalizePeriod(string? period) =>
        string.Equals(period, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";
}
