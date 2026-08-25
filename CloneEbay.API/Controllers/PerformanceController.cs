using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Performance;
using CloneEbay.Application.Sellers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

/// <summary>
/// Spec C.3 #1: the reference PerformanceController had auth commented out on every action,
/// hardcoding a demo seller GUID instead. Real [Authorize] + real sellerId from the start here.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
[Route("api/performance")]
public sealed class PerformanceController(
    IPerformanceReportService performanceReportService,
    ISellerPerformanceService sellerPerformanceService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiEnvelope<PerformanceSummaryDto>>> GetSummary([FromQuery] string? period, CancellationToken cancellationToken)
    {
        var result = await performanceReportService.GetSummaryAsync(User.GetRequiredUserId(), period, cancellationToken);
        return Ok(ApiEnvelope<PerformanceSummaryDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("sales")]
    public async Task<ActionResult<ApiEnvelope<PerformanceSalesReportDto>>> GetSales([FromQuery] string? period, CancellationToken cancellationToken)
    {
        var result = await performanceReportService.GetSalesReportAsync(User.GetRequiredUserId(), period, cancellationToken);
        return Ok(ApiEnvelope<PerformanceSalesReportDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("traffic")]
    public async Task<ActionResult<ApiEnvelope<PerformanceTrafficDto>>> GetTraffic([FromQuery] string? period, CancellationToken cancellationToken)
    {
        var result = await performanceReportService.GetTrafficAsync(User.GetRequiredUserId(), period, cancellationToken);
        return Ok(ApiEnvelope<PerformanceTrafficDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("service-metrics")]
    public async Task<ActionResult<ApiEnvelope<PerformanceServiceMetricsDto>>> GetServiceMetrics([FromQuery] string? period, CancellationToken cancellationToken)
    {
        var result = await performanceReportService.GetServiceMetricsAsync(User.GetRequiredUserId(), period, cancellationToken);
        return Ok(ApiEnvelope<PerformanceServiceMetricsDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Spec C.2 — already implemented in ISellerPerformanceService; exposed here too under the spec's literal route.</summary>
    [HttpGet("seller-level")]
    public async Task<ActionResult<ApiEnvelope<SellerPerformanceResponse>>> GetSellerLevel(CancellationToken cancellationToken)
    {
        var result = await sellerPerformanceService.GetAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<SellerPerformanceResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Spec C.3 #3: the reference project detects "no warning" by checking the concrete
    /// IActionResult type in the controller. Done cleanly here — checked on the DTO itself.
    /// </summary>
    [HttpGet("seller-standards-warning")]
    public async Task<IActionResult> GetSellerStandardsWarning(CancellationToken cancellationToken)
    {
        var result = await sellerPerformanceService.GetEarlyWarningAsync(User.GetRequiredUserId(), cancellationToken);
        if (result.Severity is null) return NoContent();
        return Ok(ApiEnvelope<SellerPerformanceWarningResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
