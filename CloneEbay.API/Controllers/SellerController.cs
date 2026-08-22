using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Sellers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Authorize]
[Route("api/seller")]
public sealed class SellerController(ISellerService sellerService) : ControllerBase
{
    [HttpPost("apply")]
    public async Task<ActionResult<ApiEnvelope<SellerVerificationResponse>>> Apply(
        SellerApplyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sellerService.ApplyAsync(User.GetRequiredUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiEnvelope<SellerVerificationResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("verification-status")]
    public async Task<ActionResult<ApiEnvelope<SellerVerificationResponse>>> Status(CancellationToken cancellationToken)
    {
        var result = await sellerService.GetVerificationStatusAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<SellerVerificationResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
