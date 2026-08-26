using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Route("coupons")]
[Authorize(Policy = "VerifiedSeller")]
public sealed class CouponsController(SellerApiClient apiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<IReadOnlyList<CouponSummaryDto>>("api/coupons", cancellationToken: cancellationToken);
        if (!result.Success) TempData["Error"] = result.ErrorMessage;
        return View(new CouponIndexViewModel { Coupons = result.Data ?? [] });
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new CouponFormViewModel { StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(30) };
        await PopulateLookupsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CouponFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model, cancellationToken);
            return View("Form", model);
        }

        var result = await apiClient.CreateCouponAsync(model, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await PopulateLookupsAsync(model, cancellationToken);
            return View("Form", model);
        }
        TempData["Success"] = "Mã giảm giá đã được tạo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<CouponDetailsDto>($"api/coupons/{id}", cancellationToken: cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
        var model = ToFormModel(result.Data);
        await PopulateLookupsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CouponFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(model, cancellationToken);
            return View("Form", model);
        }

        var result = await apiClient.UpdateCouponAsync(id, model, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await PopulateLookupsAsync(model, cancellationToken);
            return View("Form", model);
        }
        TempData["Success"] = "Mã giảm giá đã được cập nhật.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, bool isActive, CancellationToken cancellationToken)
    {
        var result = await apiClient.PatchAsync<object>($"api/coupons/{id}/status", new { isActive }, cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? (isActive ? "Mã giảm giá đã được kích hoạt." : "Mã giảm giá đã bị tạm ngưng.")
            : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.DeleteAsync<object>($"api/coupons/{id}", cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Mã giảm giá đã được xóa." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookupsAsync(CouponFormViewModel model, CancellationToken cancellationToken)
    {
        var typesTask = apiClient.GetAsync<IReadOnlyList<CouponTypeDto>>("api/coupons/types", cancellationToken: cancellationToken);
        var categoriesTask = apiClient.GetAsync<IReadOnlyList<CategoryDto>>("api/categories", false, cancellationToken);
        var productsTask = apiClient.GetAsync<PagedResultDto<ProductDto>>("api/products?pageSize=100&sortBy=updatedAt&sortOrder=desc", cancellationToken: cancellationToken);
        await Task.WhenAll(typesTask, categoriesTask, productsTask);
        model.Types = (await typesTask).Data ?? [];
        model.Categories = (await categoriesTask).Data ?? [];
        model.ProductOptions = (await productsTask).Data?.Items ?? [];
    }

    private static CouponFormViewModel ToFormModel(CouponDetailsDto coupon) => new()
    {
        Id = coupon.Id,
        CouponTypeId = coupon.CouponTypeId,
        Name = coupon.Name,
        Code = coupon.Code,
        DiscountValue = coupon.DiscountValue,
        DiscountUnit = coupon.DiscountUnit,
        MaxDiscount = coupon.MaxDiscount,
        StartDate = coupon.StartDate.ToLocalTime(),
        EndDate = coupon.EndDate.ToLocalTime(),
        UsageLimit = coupon.UsageLimit,
        UsagePerUser = coupon.UsagePerUser,
        MinimumOrderValue = coupon.MinimumOrderValue,
        ApplicablePriceMin = coupon.ApplicablePriceMin,
        ApplicablePriceMax = coupon.ApplicablePriceMax,
        CategoryId = coupon.CategoryId,
        ProductId = coupon.ProductId,
        IsActive = coupon.IsActive,
        BuyQuantity = coupon.Condition?.BuyQuantity,
        GetQuantity = coupon.Condition?.GetQuantity,
        GetDiscountPercent = coupon.Condition?.GetDiscountPercent,
        SaveEveryAmount = coupon.Condition?.SaveEveryAmount,
        SaveEveryItems = coupon.Condition?.SaveEveryItems,
        ConditionDescription = coupon.Condition?.ConditionDescription
    };
}
