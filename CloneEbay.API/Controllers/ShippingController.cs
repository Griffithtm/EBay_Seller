using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
[Route("api/shipping")]
public sealed class ShippingController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<ApiEnvelope<ShippingInfoSummaryDto>>> GetShipping(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.GetShippingInfoAsync(orderId, currentSellerId, cancellationToken);
        return Ok(ApiEnvelope<ShippingInfoSummaryDto>.Ok(result, HttpContext.TraceIdentifier));
    }
}
