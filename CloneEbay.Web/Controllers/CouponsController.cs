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
    public async Task<IActionResult> Index(int? productId, string? status, CancellationToken cancellationToken)
    {
        var query = "api/coupons";
        var filters = new List<string>();
        if (productId.HasValue) filters.Add($"productId={productId.Value}");
        if (!string.IsNullOrWhiteSpace(status)) filters.Add($"status={Uri.EscapeDataString(status)}");
        if (filters.Count > 0) query += "?" + string.Join("&", filters);

        var couponsTask = apiClient.GetAsync<IReadOnlyList<CouponDto>>(query, cancellationToken: cancellationToken);
        var productsTask = apiClient.GetAsync<PagedResultDto<ProductDto>>("api/products?pageSize=100&sortBy=updatedAt&sortOrder=desc", cancellationToken: cancellationToken);
        await Task.WhenAll(couponsTask, productsTask);
        var couponResult = await couponsTask;
        var productResult = await productsTask;
        if (!couponResult.Success) TempData["Error"] = couponResult.ErrorMessage;

        return View(new CouponIndexViewModel
        {
            Coupons = couponResult.Data ?? [],
            Products = productResult.Data?.Items ?? [],
            ProductId = productId,
            Status = status
        });
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new CouponFormViewModel();
        await PopulateProductsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CouponFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateCouponForm(model);
        if (!ModelState.IsValid)
        {
            await PopulateProductsAsync(model, cancellationToken);
            return View("Form", model);
        }

        var result = await apiClient.CreateCouponAsync(model, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await PopulateProductsAsync(model, cancellationToken);
            return View("Form", model);
        }
        TempData["Success"] = "Mã giảm giá đã được tạo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<CouponDto>($"api/coupons/{id}", cancellationToken: cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
        var model = ToFormModel(result.Data);
        await PopulateProductsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CouponFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        ValidateCouponForm(model);
        if (!ModelState.IsValid)
        {
            await PopulateProductsAsync(model, cancellationToken);
            return View("Form", model);
        }

        var result = await apiClient.UpdateCouponAsync(id, model, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await PopulateProductsAsync(model, cancellationToken);
            return View("Form", model);
        }
        TempData["Success"] = "Mã giảm giá đã được cập nhật.";
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

    private async Task PopulateProductsAsync(CouponFormViewModel model, CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<PagedResultDto<ProductDto>>("api/products?pageSize=100&sortBy=updatedAt&sortOrder=desc", cancellationToken: cancellationToken);
        model.ProductOptions = result.Data?.Items ?? [];
    }

    private void ValidateCouponForm(CouponFormViewModel model)
    {
        if (model.EndDate <= model.StartDate)
            ModelState.AddModelError(nameof(model.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
    }

    private static CouponFormViewModel ToFormModel(CouponDto coupon) => new()
    {
        Id = coupon.Id,
        ProductId = coupon.ProductId ?? 0,
        Code = coupon.Code,
        DiscountPercent = coupon.DiscountPercent,
        StartDate = coupon.StartDate.ToLocalTime(),
        EndDate = coupon.EndDate.ToLocalTime(),
        MaxUsage = coupon.MaxUsage
    };
}
