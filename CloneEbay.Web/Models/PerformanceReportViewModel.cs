namespace CloneEbay.Web.Models;

public sealed class PerformanceReportViewModel
{
    public string Period { get; set; } = "week";
    public PerformanceSummaryDto? Summary { get; set; }
    public PerformanceSalesReportDto? Sales { get; set; }
    public PerformanceTrafficDto? Traffic { get; set; }
    public PerformanceServiceMetricsDto? ServiceMetrics { get; set; }
}
