using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Route("dashboard")]
public sealed class DashboardController(SellerApiClient apiClient) : Controller
{
    [Authorize(Policy = "VerifiedSeller")]
    [HttpGet("")]
    public async Task<IActionResult> Index(string? period, CancellationToken cancellationToken = default)
    {
        var normalizedPeriod = string.Equals(period, "month", StringComparison.OrdinalIgnoreCase) ? "month" : "week";

        var summaryTask = apiClient.GetAsync<DashboardSummaryDto>($"api/dashboard/summary?period={normalizedPeriod}", cancellationToken: cancellationToken);
        var topProductsTask = apiClient.GetAsync<IReadOnlyList<TopProductDto>>($"api/dashboard/top-products?period={normalizedPeriod}&limit=10", cancellationToken: cancellationToken);
        var chartTask = apiClient.GetAsync<RevenueChartDto>($"api/dashboard/revenue-chart?period={normalizedPeriod}", cancellationToken: cancellationToken);
        var performanceTask = apiClient.GetAsync<SellerPerformanceDto>($"api/dashboard/performance?period={normalizedPeriod}", cancellationToken: cancellationToken);

        await Task.WhenAll(summaryTask, topProductsTask, chartTask, performanceTask);

        var summaryRes = await summaryTask;
        var topProductsRes = await topProductsTask;
        var chartRes = await chartTask;
        var perfRes = await performanceTask;

        if (!summaryRes.Success && summaryRes.ErrorMessage is not null)
        {
            TempData["Error"] = summaryRes.ErrorMessage;
        }

        var defaultFrom = DateTime.UtcNow.Date.AddDays(-7);
        var defaultTo = DateTime.UtcNow.Date;

        var model = new DashboardIndexViewModel
        {
            Period = normalizedPeriod,
            Summary = summaryRes.Data ?? new DashboardSummaryDto(0m, 0, 0m, normalizedPeriod, defaultFrom, defaultTo),
            TopProducts = topProductsRes.Data ?? [],
            RevenueChart = chartRes.Data ?? new RevenueChartDto(normalizedPeriod, 0m, 0, []),
            Performance = perfRes.Data ?? new SellerPerformanceDto(normalizedPeriod, 0, 0m, 0m, "AboveStandard", 100m)
        };

        return View(model);
    }
}