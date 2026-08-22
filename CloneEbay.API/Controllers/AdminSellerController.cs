using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Common;
using CloneEbay.Application.Sellers;
using CloneEbay.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/seller")]
public sealed class AdminSellerController(ISellerService sellerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<PagedResult<SellerApplicationResponse>>>> GetApplications(
        [FromQuery] VerificationStatus? status = VerificationStatus.Pending,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sellerService.GetApplicationsAsync(status, page, pageSize, cancellationToken);
        return Ok(ApiEnvelope<PagedResult<SellerApplicationResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/approve")]
    public async Task<ActionResult<ApiEnvelope<SellerApplicationResponse>>> Approve(int id, CancellationToken cancellationToken)
    {
        var result = await sellerService.ApproveAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<SellerApplicationResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/reject")]
    public async Task<ActionResult<ApiEnvelope<SellerApplicationResponse>>> Reject(
        int id,
        RejectSellerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sellerService.RejectAsync(id, User.GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiEnvelope<SellerApplicationResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
