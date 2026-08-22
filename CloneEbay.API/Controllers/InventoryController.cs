using System.ComponentModel.DataAnnotations;
using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
[Route("api/inventory")]
public sealed class InventoryController(IProductService productService) : ControllerBase
{
    [HttpGet("{productId:int}")]
    public async Task<ActionResult<ApiEnvelope<InventoryResponse>>> Get(int productId, CancellationToken cancellationToken)
    {
        var result = await productService.GetInventoryAsync(productId, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<InventoryResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{productId:int}")]
    public async Task<ActionResult<ApiEnvelope<InventoryResponse>>> Update(
        int productId,
        [FromBody] InventoryUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.UpdateInventoryAsync(productId, User.GetRequiredUserId(), request.Quantity, cancellationToken);
        return Ok(ApiEnvelope<InventoryResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}

public sealed record InventoryUpdateRequest([Range(0, int.MaxValue)] int Quantity);
