using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Common;
using CloneEbay.Application.Returns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/returns")]
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
public class ReturnsController(IReturnService returnService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<PagedResult<ReturnRequestResponse>>>> GetReturns(
        [FromQuery] string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 7,
        CancellationToken cancellationToken = default)
    {
        var query = new ReturnRequestListQuery(User.GetRequiredUserId(), status, fromDate, toDate, searchTerm, page, pageSize);
        var result = await returnService.GetReturnsAsync(query, cancellationToken);
        return Ok(ApiEnvelope<PagedResult<ReturnRequestResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ApiEnvelope<ReturnRequestResponse>>> UpdateReturnStatus(
        int id,
        [FromBody] UpdateReturnStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnService.UpdateReturnStatusAsync(id, User.GetRequiredUserId(), request.Status, cancellationToken);
        return Ok(ApiEnvelope<ReturnRequestResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}

