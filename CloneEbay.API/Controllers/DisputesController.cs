using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Common;
using CloneEbay.Application.Disputes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/disputes")]
[Authorize]
public class DisputesController(IDisputeService disputeService, IWebHostEnvironment env) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<DisputeResponse>>> CreateDispute(
        [FromBody] CreateDisputeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await disputeService.CreateDisputeAsync(User.GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiEnvelope<DisputeResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<PagedResult<DisputeResponse>>>> GetDisputes(
        [FromQuery] string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 7,
        CancellationToken cancellationToken = default)
    {
        var query = new DisputeListQuery(User.GetRequiredUserId(), status, fromDate, toDate, searchTerm, page, pageSize);
        var result = await disputeService.GetDisputesAsync(query, cancellationToken);
        return Ok(ApiEnvelope<PagedResult<DisputeResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiEnvelope<DisputeResponse>>> GetDisputeById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await disputeService.GetDisputeByIdAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<DisputeResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/respond")]
    public async Task<ActionResult<ApiEnvelope<DisputeResponse>>> RespondToDispute(
        int id,
        [FromBody] RespondToDisputeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await disputeService.RespondToDisputeAsync(id, User.GetRequiredUserId(), request.Message, cancellationToken);
        return Ok(ApiEnvelope<DisputeResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/evidence")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    public async Task<ActionResult<ApiEnvelope<DisputeResponse>>> UploadEvidence(
        int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiEnvelope<object>.Failure("NO_FILE", "File is empty.", HttpContext.TraceIdentifier));

        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "disputes");
        if (!Directory.Exists(uploadsDir))
            Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativePath = $"/uploads/disputes/{fileName}";
        var result = await disputeService.UploadEvidenceAsync(
            id, User.GetRequiredUserId(), relativePath, file.FileName, file.ContentType, cancellationToken);

        return Ok(ApiEnvelope<DisputeResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/escalate")]
    public async Task<ActionResult<ApiEnvelope<DisputeResponse>>> EscalateDispute(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await disputeService.EscalateDisputeAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<DisputeResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/resolve")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<DisputeResponse>>> ResolveDispute(
        int id,
        [FromBody] ResolveDisputeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await disputeService.ResolveDisputeAsync(id, User.GetRequiredUserId(), request.Resolution, cancellationToken);
        return Ok(ApiEnvelope<DisputeResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/close")]
    public async Task<ActionResult<ApiEnvelope<DisputeResponse>>> CloseDispute(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await disputeService.CloseDisputeAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<DisputeResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
