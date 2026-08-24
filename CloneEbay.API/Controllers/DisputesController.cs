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
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
public class DisputesController(IDisputeService disputeService) : ControllerBase
{
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

    [HttpPut("{id:int}/resolve")]
    public async Task<ActionResult<ApiEnvelope<DisputeResponse>>> ResolveDispute(
        int id,
        [FromBody] ResolveDisputeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await disputeService.ResolveDisputeAsync(id, User.GetRequiredUserId(), request.Resolution, cancellationToken);
        return Ok(ApiEnvelope<DisputeResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}

