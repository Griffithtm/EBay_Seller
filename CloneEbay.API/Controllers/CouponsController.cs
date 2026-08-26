using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/coupons")]
[Authorize]
public sealed class CouponsController(ICouponService couponService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<CouponSummaryResponse>>>> List(CancellationToken cancellationToken)
    {
        var result = await couponService.GetForSellerAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<IReadOnlyList<CouponSummaryResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("types")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<CouponTypeResponse>>>> Types(CancellationToken cancellationToken)
    {
        var result = await couponService.GetTypesAsync(cancellationToken);
        return Ok(ApiEnvelope<IReadOnlyList<CouponTypeResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiEnvelope<CouponDetailsResponse>>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await couponService.GetAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<CouponDetailsResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<ApiEnvelope<CouponDetailsResponse>>> GetByCode(string code, CancellationToken cancellationToken)
    {
        var result = await couponService.GetByCodeAsync(User.GetRequiredUserId(), code, cancellationToken);
        return Ok(ApiEnvelope<CouponDetailsResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<CouponDetailsResponse>>> Create(
        [FromBody] CreateCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await couponService.CreateAsync(User.GetRequiredUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, ApiEnvelope<CouponDetailsResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<CouponDetailsResponse>>> Update(
        int id,
        [FromBody] UpdateCouponRequest request,
        CancellationToken cancellationToken)
    {
        var result = await couponService.UpdateAsync(id, User.GetRequiredUserId(), request, cancellationToken);
        return Ok(ApiEnvelope<CouponDetailsResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateCouponStatusRequest request,
        CancellationToken cancellationToken)
    {
        await couponService.UpdateStatusAsync(User.GetRequiredUserId(), id, request.IsActive, cancellationToken);
        return Ok(ApiEnvelope<object>.Ok(new { }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await couponService.DeleteAsync(User.GetRequiredUserId(), id, cancellationToken);
        return NoContent();
    }

    /// <summary>Checkout-time validity check for whatever builds the buyer-facing order flow next.</summary>
    [HttpGet("validate")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiEnvelope<ValidateCouponResponse>>> Validate(
        [FromQuery] string? code,
        [FromQuery] int? productId,
        [FromQuery] decimal? orderTotal,
        CancellationToken cancellationToken)
    {
        var result = await couponService.ValidateAsync(code, productId, orderTotal, cancellationToken);
        return Ok(ApiEnvelope<ValidateCouponResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
