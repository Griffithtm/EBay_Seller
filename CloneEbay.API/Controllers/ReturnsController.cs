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
[Authorize]
public class ReturnsController(IReturnService returnService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<ReturnRequestResponse>>> CreateReturnRequest(
        [FromBody] CreateReturnRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnService.CreateReturnRequestAsync(User.GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiEnvelope<ReturnRequestResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiEnvelope<ReturnRequestResponse>>> GetReturnById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await returnService.GetReturnByIdAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<ReturnRequestResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<ReturnRequestResponse>>> UpdateReturnStatus(
        int id,
        [FromBody] UpdateReturnStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnService.UpdateReturnStatusAsync(id, User.GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiEnvelope<ReturnRequestResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/shipping")]
    public async Task<ActionResult<ApiEnvelope<ReturnRequestResponse>>> ProvideReturnShipping(
        int id,
        [FromBody] ProvideReturnShippingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnService.ProvideReturnShippingAsync(id, User.GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiEnvelope<ReturnRequestResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/replacement")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<ReturnRequestResponse>>> SendReplacement(
        int id,
        [FromBody] SendReplacementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnService.SendReplacementAsync(id, User.GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiEnvelope<ReturnRequestResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
