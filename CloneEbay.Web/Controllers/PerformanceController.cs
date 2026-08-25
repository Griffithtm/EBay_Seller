using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Route("performance")]
[Authorize(Policy = "VerifiedSeller")]
public sealed class PerformanceController(SellerApiClient apiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string period = "week", CancellationToken cancellationToken = default)
    {
        var summaryTask = apiClient.GetPerformanceSummaryAsync(period, cancellationToken);
        var salesTask = apiClient.GetPerformanceSalesAsync(period, cancellationToken);
        var trafficTask = apiClient.GetPerformanceTrafficAsync(period, cancellationToken);
        var metricsTask = apiClient.GetPerformanceServiceMetricsAsync(period, cancellationToken);
        await Task.WhenAll(summaryTask, salesTask, trafficTask, metricsTask);

        var summaryResult = await summaryTask;
        var salesResult = await salesTask;
        var trafficResult = await trafficTask;
        var metricsResult = await metricsTask;

        if (!summaryResult.Success) TempData["Error"] = summaryResult.ErrorMessage;

        return View(new PerformanceReportViewModel
        {
            Period = period,
            Summary = summaryResult.Data,
            Sales = salesResult.Data,
            Traffic = trafficResult.Data,
            ServiceMetrics = metricsResult.Data
        });
    }
}
