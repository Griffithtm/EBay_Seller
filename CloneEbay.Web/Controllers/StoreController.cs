using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Route("store")]
public sealed class StoreController(SellerApiClient apiClient) : Controller
{
    [AllowAnonymous]
    [HttpGet("{sellerId:int}")]
    public async Task<IActionResult> Public(int sellerId, CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<StoreProfileDto>($"api/store/{sellerId}", false, cancellationToken);
        return result.Success && result.Data is not null ? View(result.Data) : NotFound();
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpGet("edit")]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<StoreProfileDto>("api/store/me", cancellationToken: cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Seller");
        }

        return View(new StoreProfileViewModel
        {
            SellerId = result.Data.SellerId,
            StoreName = result.Data.StoreName,
            Description = result.Data.Description,
            CurrentBannerImageUrl = result.Data.BannerImageUrl
        });
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Edit(StoreProfileViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await apiClient.UpdateStoreAsync(model, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(model);
        }

        TempData["Success"] = "Hồ sơ cửa hàng đã được cập nhật.";
        return RedirectToAction(nameof(Edit));
    }
}
