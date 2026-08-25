using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Reviews;
using CloneEbay.Application.Sellers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Authorize]
[Route("api/seller")]
public sealed class SellerController(ISellerService sellerService, ISellerPerformanceService performanceService) : ControllerBase
{
    [HttpGet("performance")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<SellerPerformanceResponse>>> Performance(CancellationToken cancellationToken)
    {
        var result = await performanceService.GetAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<SellerPerformanceResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("performance/warnings")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<SellerPerformanceWarningResponse>>> PerformanceWarnings(CancellationToken cancellationToken)
    {
        var result = await performanceService.GetEarlyWarningAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<SellerPerformanceWarningResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

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

    [HttpGet("buyer-summary/{buyerId:int}")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<BuyerSummaryResponse>>> GetBuyerSummary(
        int buyerId,
        [FromServices] CloneEbay.Application.Reviews.IReviewService reviewService,
        CancellationToken cancellationToken)
    {
        var result = await reviewService.GetBuyerSummaryAsync(User.GetRequiredUserId(), buyerId, cancellationToken);
        return Ok(ApiEnvelope<BuyerSummaryResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
