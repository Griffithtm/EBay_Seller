using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.API.Storage;
using CloneEbay.Application.Common;
using CloneEbay.Application.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/store")]
public sealed class StoreController(IStoreService storeService, IStoreBannerStorage bannerStorage) : ControllerBase
{
    [HttpGet("{sellerId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiEnvelope<StoreProfileResponse>>> GetPublic(int sellerId, CancellationToken cancellationToken)
    {
        var result = await storeService.GetPublicAsync(sellerId, cancellationToken);
        return Ok(ApiEnvelope<StoreProfileResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiEnvelope<StoreProfileResponse>>> GetMine(CancellationToken cancellationToken)
    {
        var result = await storeService.GetMineAsync(User.GetRequiredUserId(), cancellationToken);
        return Ok(ApiEnvelope<StoreProfileResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    [Consumes("application/json", "multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ApiEnvelope<StoreProfileResponse>>> Update(
        CancellationToken cancellationToken)
    {
        string? storeName;
        string? description;
        string? bannerUrl;

        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync(cancellationToken);
            storeName = form.ContainsKey("storeName") ? form["storeName"].ToString() : null;
            description = form.ContainsKey("description") ? form["description"].ToString() : null;
            bannerUrl = form.ContainsKey("bannerImageURL") ? form["bannerImageURL"].ToString() : null;
            var banner = form.Files.GetFile("banner");
            if (banner is not null)
            {
                bannerUrl = await bannerStorage.SaveAsync(banner, cancellationToken);
            }
        }
        else if (Request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) == true)
        {
            var request = await Request.ReadFromJsonAsync<UpdateStoreProfileRequest>(cancellationToken)
                ?? throw new AppProblemException(400, "VALIDATION_ERROR", "A request body is required.");
            storeName = request.StoreName;
            description = request.Description;
            bannerUrl = request.BannerImageUrl;
        }
        else
        {
            throw new AppProblemException(415, "UNSUPPORTED_MEDIA_TYPE", "Use application/json or multipart/form-data.");
        }

        if (storeName is null && description is null && bannerUrl is null)
        {
            throw new AppProblemException(422, "NO_FIELDS_TO_UPDATE", "Provide at least one store profile field.");
        }

        var result = await storeService.UpdateAsync(
            User.GetRequiredUserId(),
            new UpdateStoreProfileRequest(storeName, description, bannerUrl),
            cancellationToken);
        return Ok(ApiEnvelope<StoreProfileResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
