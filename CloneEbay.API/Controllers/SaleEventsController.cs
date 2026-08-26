using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.SaleEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/sale-events")]
[Authorize]
public sealed class SaleEventsController(ISaleEventService saleEventService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyList<SaleEventSummaryResponse>>>> List(CancellationToken cancellationToken)
    {
        var result = await saleEventService.GetForSellerAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<IReadOnlyList<SaleEventSummaryResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("eligible-products")]
    public async Task<ActionResult<ApiEnvelope<EligibleProductsPageResponse>>> EligibleProducts(
        [FromQuery] string? searchTerm,
        [FromQuery] int? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? excludeSaleEventId,
        [FromQuery] bool excludeAlreadyAssigned = true,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await saleEventService.GetEligibleProductsAsync(
            User.GetRequiredUserId(),
            excludeSaleEventId,
            new EligibleProductQuery(searchTerm, categoryId, minPrice, maxPrice, excludeAlreadyAssigned, pageNumber, pageSize),
            cancellationToken);
        return Ok(ApiEnvelope<EligibleProductsPageResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiEnvelope<SaleEventDetailResponse?>>> Get(int id, CancellationToken cancellationToken)
    {
        var result = await saleEventService.GetByIdAsync(User.GetRequiredUserId(), id, cancellationToken);
        return Ok(ApiEnvelope<SaleEventDetailResponse?>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<SaleEventDetailResponse>>> Create(
        [FromBody] CreateSaleEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await saleEventService.CreateAsync(User.GetRequiredUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, ApiEnvelope<SaleEventDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<SaleEventDetailResponse>>> Update(
        int id,
        [FromBody] UpdateSaleEventRequest request,
        CancellationToken cancellationToken)
    {
        var result = await saleEventService.UpdateAsync(User.GetRequiredUserId(), id, request, cancellationToken);
        return Ok(ApiEnvelope<SaleEventDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateSaleEventStatusRequest request,
        CancellationToken cancellationToken)
    {
        await saleEventService.UpdateStatusAsync(User.GetRequiredUserId(), id, request.Status, cancellationToken);
        return Ok(ApiEnvelope<object>.Ok(new { }, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await saleEventService.DeleteAsync(User.GetRequiredUserId(), id, cancellationToken);
        return NoContent();
    }
}
