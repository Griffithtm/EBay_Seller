using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Common;
using CloneEbay.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
[Route("api/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpPost("downloads")]
    public async Task<ActionResult<ApiEnvelope<ReportDownloadResponse>>> CreateDownload(
        CreateReportDownloadRequest request, CancellationToken cancellationToken)
    {
        var result = await reportService.CreateDownloadAsync(User.GetRequiredUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiEnvelope<ReportDownloadResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("downloads")]
    public async Task<ActionResult<ApiEnvelope<PagedResult<ReportDownloadResponse>>>> ListDownloads(
        [FromQuery] string? source,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await reportService.ListDownloadsAsync(
            User.GetRequiredUserId(),
            new ReportDownloadListQuery(source, status, fromUtc, toUtc, page, pageSize),
            cancellationToken);
        return Ok(ApiEnvelope<PagedResult<ReportDownloadResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("schedules")]
    public async Task<ActionResult<ApiEnvelope<ReportScheduleResponse>>> CreateSchedule(
        CreateReportScheduleRequest request, CancellationToken cancellationToken)
    {
        var result = await reportService.CreateScheduleAsync(User.GetRequiredUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiEnvelope<ReportScheduleResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("schedules")]
    public async Task<ActionResult<ApiEnvelope<PagedResult<ReportScheduleResponse>>>> ListSchedules(
        [FromQuery] string? source,
        [FromQuery] string? type,
        [FromQuery] string? frequency,
        [FromQuery] bool onlyActive = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await reportService.ListSchedulesAsync(
            User.GetRequiredUserId(),
            new ReportScheduleListQuery(source, type, frequency, onlyActive, page, pageSize),
            cancellationToken);
        return Ok(ApiEnvelope<PagedResult<ReportScheduleResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("schedules/{id:int}/deactivate")]
    public async Task<ActionResult<ApiEnvelope<ReportScheduleResponse>>> DeactivateSchedule(int id, CancellationToken cancellationToken)
    {
        var result = await reportService.DeactivateScheduleAsync(User.GetRequiredUserId(), id, cancellationToken);
        return Ok(ApiEnvelope<ReportScheduleResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
