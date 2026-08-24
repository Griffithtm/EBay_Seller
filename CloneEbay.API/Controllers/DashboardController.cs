using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiEnvelope<DashboardSummaryDto>>> GetSummary(
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        var sellerId = User.GetRequiredUserId();
        var result = await dashboardService.GetSummaryAsync(sellerId, period, cancellationToken);
        return Ok(ApiEnvelope<DashboardSummaryDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("top-products")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<TopProductDto>>>> GetTopProducts(
        [FromQuery] string? period,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var sellerId = User.GetRequiredUserId();
        var result = await dashboardService.GetTopProductsAsync(sellerId, period, limit, cancellationToken);
        return Ok(ApiEnvelope<IReadOnlyList<TopProductDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("revenue-chart")]
    public async Task<ActionResult<ApiEnvelope<RevenueChartDto>>> GetRevenueChart(
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        var sellerId = User.GetRequiredUserId();
        var result = await dashboardService.GetRevenueChartAsync(sellerId, period, cancellationToken);
        return Ok(ApiEnvelope<RevenueChartDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("performance")]
    public async Task<ActionResult<ApiEnvelope<SellerPerformanceDto>>> GetPerformance(
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        var sellerId = User.GetRequiredUserId();
        var result = await dashboardService.GetPerformanceAsync(sellerId, period, cancellationToken);
        return Ok(ApiEnvelope<SellerPerformanceDto>.Ok(result, HttpContext.TraceIdentifier));
    }
}