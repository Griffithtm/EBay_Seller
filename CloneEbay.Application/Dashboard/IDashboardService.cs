namespace CloneEbay.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int sellerId, string? period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(int sellerId, string? period, int limit = 10, CancellationToken cancellationToken = default);
    Task<RevenueChartDto> GetRevenueChartAsync(int sellerId, string? period, CancellationToken cancellationToken = default);
    Task<SellerPerformanceDto> GetPerformanceAsync(int sellerId, string? period, CancellationToken cancellationToken = default);
}