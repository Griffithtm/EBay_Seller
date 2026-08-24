using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CloneEbay.Web.Controllers;

[Authorize]
[Route("seller")]
public sealed class SellerController(SellerApiClient apiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<SellerVerificationDto>("api/seller/verification-status", cancellationToken: cancellationToken);
        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            return View(new SellerVerificationDto(false, null, null, null, null, null, null));
        }
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else if (result.Data?.Status == "Approved" && !User.HasClaim("verified", "true") &&
                 await apiClient.RefreshSessionAsync(cancellationToken))
        {
            return RedirectToAction(nameof(Index));
        }
        return View(result.Data);
    }

    [HttpGet("apply")]
    public IActionResult Apply() => View(new SellerApplyViewModel());

    [HttpPost("apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(SellerApplyViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await apiClient.PostAsync<SellerVerificationDto>(
            "api/seller/apply",
            new { model.SellerType, model.LegalName, Phone = model.ContactPhone, model.StoreName },
            cancellationToken: cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return View(model);
        }

        TempData["Success"] = "Your seller application was submitted for review.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpGet("buyer-summary/{buyerId:int}")]
    public async Task<IActionResult> GetBuyerSummary(int buyerId, CancellationToken cancellationToken)
    {
        var result = await apiClient.GetBuyerSummaryAsync(buyerId, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return Json(new { success = false, message = result.ErrorMessage ?? "Không thể lấy thông tin người mua." });
        }
        return Json(new { success = true, data = result.Data });
    }
}
