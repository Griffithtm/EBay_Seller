using System.ComponentModel.DataAnnotations;
using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.API.Storage;
using CloneEbay.Application.Common;
using CloneEbay.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(
    IProductService productService,
    IProductImageStorage imageStorage,
    IProductCreateRateLimiter productCreateRateLimiter) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiEnvelope<PagedResult<ProductResponse>>>> List(
        [FromQuery] int? sellerId,
        [FromQuery] string? status,
        [FromQuery] int? categoryId,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        CancellationToken cancellationToken = default)
    {
        var actorId = User.Identity?.IsAuthenticated == true ? User.GetRequiredUserId() : (int?)null;
        var verifiedSeller = actorId.HasValue && User.IsInRole("Seller") && User.HasClaim("verified", "true");
        var effectiveSellerId = verifiedSeller && !sellerId.HasValue ? actorId : sellerId;
        var includeHidden = verifiedSeller && effectiveSellerId == actorId;
        var result = await productService.ListAsync(
            new ProductListQuery(effectiveSellerId, status, categoryId, keyword, page, pageSize, sortBy, sortOrder),
            includeHidden,
            cancellationToken);
        return Ok(ApiEnvelope<PagedResult<ProductResponse>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiEnvelope<ProductResponse>>> Get(int id, CancellationToken cancellationToken)
    {
        var actorId = User.Identity?.IsAuthenticated == true ? User.GetRequiredUserId() : (int?)null;
        var result = await productService.GetAsync(id, actorId, User.IsInRole("Admin"), cancellationToken);
        return Ok(ApiEnvelope<ProductResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(45 * 1024 * 1024)]
    public async Task<ActionResult<ApiEnvelope<ProductResponse>>> Create(
        [FromForm] ProductCreateForm form,
        CancellationToken cancellationToken)
    {
        var quota = await productCreateRateLimiter.AcquireAsync(User.GetRequiredUserId(), cancellationToken);
        Response.Headers["X-RateLimit-Remaining"] = quota.Remaining.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!quota.IsAllowed)
        {
            Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(quota.RetryAfter.TotalSeconds))
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            throw new AppProblemException(429, "PRODUCT_CREATE_RATE_LIMIT", "Product creation limit reached. Try again later.");
        }
        var images = await imageStorage.SaveManyAsync(form.Images, cancellationToken);
        try
        {
            var result = await productService.CreateAsync(
                User.GetRequiredUserId(),
                new CreateProductRequest(form.Title, form.Description, form.Price, form.CategoryId, form.IsAuction, form.AuctionEndTime, form.Quantity, images),
                cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, ApiEnvelope<ProductResponse>.Ok(result, HttpContext.TraceIdentifier));
        }
        catch
        {
            await imageStorage.DeleteManyAsync(images, CancellationToken.None);
            throw;
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(45 * 1024 * 1024)]
    public async Task<ActionResult<ApiEnvelope<ProductResponse>>> Update(
        int id,
        [FromForm] ProductUpdateForm form,
        CancellationToken cancellationToken)
    {
        var current = await productService.GetAsync(id, User.GetRequiredUserId(), false, cancellationToken);
        IReadOnlyList<string>? images = null;
        if (form.Images.Count > 0) images = await imageStorage.SaveManyAsync(form.Images, cancellationToken);
        try
        {
            var result = await productService.UpdateAsync(
                id,
                User.GetRequiredUserId(),
                new UpdateProductRequest(form.Title, form.Description, form.Price, form.CategoryId, form.IsAuction, form.AuctionEndTime, form.Quantity, images),
                cancellationToken);
            if (images is not null) await imageStorage.DeleteManyAsync(current.Images, CancellationToken.None);
            return Ok(ApiEnvelope<ProductResponse>.Ok(result, HttpContext.TraceIdentifier));
        }
        catch
        {
            if (images is not null) await imageStorage.DeleteManyAsync(images, CancellationToken.None);
            throw;
        }
    }

    [HttpPatch("{id:int}/hide")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<ProductResponse>>> ToggleVisibility(int id, CancellationToken cancellationToken)
    {
        var result = await productService.ToggleVisibilityAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<ProductResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var current = await productService.GetAsync(id, User.GetRequiredUserId(), false, cancellationToken);
        await productService.DeleteAsync(id, User.GetRequiredUserId(), cancellationToken);
        await imageStorage.DeleteManyAsync(current.Images, CancellationToken.None);
        return NoContent();
    }

    [HttpPost("{id:int}/bids")]
    [Authorize]
    public async Task<ActionResult<ApiEnvelope<BidResponse>>> PlaceBid(
        int id,
        [FromBody] PlaceBidRequest request,
        CancellationToken cancellationToken)
    {
        var result = await productService.PlaceBidAsync(id, User.GetRequiredUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiEnvelope<BidResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}/bids")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<BidListResponse>>> Bids(int id, CancellationToken cancellationToken)
    {
        var result = await productService.GetBidsAsync(id, User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<BidListResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/close-auction")]
    [HttpPost("{id:int}/auction/close")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<AuctionCloseResponse>>> CloseAuction(int id, CancellationToken cancellationToken)
    {
        var result = await productService.CloseAuctionAsync(id, User.GetRequiredUserId(), false, cancellationToken);
        return Ok(ApiEnvelope<AuctionCloseResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}

public sealed class ProductCreateForm
{
    [Required, StringLength(255, MinimumLength = 3)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(4000, MinimumLength = 10)] public string Description { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal Price { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    public bool IsAuction { get; set; }
    public DateTime? AuctionEndTime { get; set; }
    [Range(0, int.MaxValue)] public int Quantity { get; set; }
    public List<IFormFile> Images { get; set; } = [];
}

public sealed class ProductUpdateForm
{
    [StringLength(255, MinimumLength = 3)] public string? Title { get; set; }
    [StringLength(4000, MinimumLength = 10)] public string? Description { get; set; }
    [Range(typeof(decimal), "0.01", "9999999999999999")] public decimal? Price { get; set; }
    [Range(1, int.MaxValue)] public int? CategoryId { get; set; }
    public bool? IsAuction { get; set; }
    public DateTime? AuctionEndTime { get; set; }
    [Range(0, int.MaxValue)] public int? Quantity { get; set; }
    public List<IFormFile> Images { get; set; } = [];
}
