namespace CloneEbay.Application.Performance;

public interface IPerformanceReportService
{
    Task<PerformanceSummaryDto> GetSummaryAsync(int sellerId, string? period, CancellationToken cancellationToken = default);

    Task<PerformanceSalesReportDto> GetSalesReportAsync(int sellerId, string? period, CancellationToken cancellationToken = default);

    Task<PerformanceTrafficDto> GetTrafficAsync(int sellerId, string? period, CancellationToken cancellationToken = default);

    Task<PerformanceServiceMetricsDto> GetServiceMetricsAsync(int sellerId, string? period, CancellationToken cancellationToken = default);
}
