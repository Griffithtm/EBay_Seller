using System.Security.Claims;
using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Route("products")]
public sealed class ProductsController(SellerApiClient apiClient) : Controller
{
    [Authorize(Policy = "VerifiedSeller")]
    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? keyword,
        string? status,
        int? categoryId,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = $"api/products?page={Math.Max(page, 1)}&pageSize=12&sortBy=updatedAt&sortOrder=desc";
        if (!string.IsNullOrWhiteSpace(keyword)) query += $"&keyword={Uri.EscapeDataString(keyword)}";
        if (!string.IsNullOrWhiteSpace(status)) query += $"&status={Uri.EscapeDataString(status)}";
        if (categoryId.HasValue) query += $"&categoryId={categoryId.Value}";

        var productsTask = apiClient.GetAsync<PagedResultDto<ProductDto>>(query, cancellationToken: cancellationToken);
        var categoriesTask = apiClient.GetAsync<IReadOnlyList<CategoryDto>>("api/categories", false, cancellationToken);
        await Task.WhenAll(productsTask, categoriesTask);
        var productResult = await productsTask;
        var categoryResult = await categoriesTask;
        if (!productResult.Success) TempData["Error"] = productResult.ErrorMessage;

        return View(new ProductIndexViewModel
        {
            Products = productResult.Data?.Items ?? [],
            Pagination = productResult.Data?.Pagination ?? new PaginationDto(1, 12, 0, 0),
            Categories = categoryResult.Data ?? [],
            Keyword = keyword,
            Status = status,
            CategoryId = categoryId
        });
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ProductFormViewModel { Quantity = 1, AuctionEndTime = DateTime.Now.AddDays(3) };
        await PopulateCategoriesAsync(model, cancellationToken);
        return View("Form", model);
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(45 * 1024 * 1024)]
    public async Task<IActionResult> Create(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        ValidateProductForm(model, requireImages: true);
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(model, cancellationToken);
            return View("Form", model);
        }

        var result = await apiClient.CreateProductAsync(model, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await PopulateCategoriesAsync(model, cancellationToken);
            return View("Form", model);
        }
        TempData["Success"] = "Sản phẩm đã được đăng và tồn kho đã được khởi tạo.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<ProductDto>($"api/products/{id}", cancellationToken: cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
        var model = ToFormModel(result.Data);
        await PopulateCategoriesAsync(model, cancellationToken);
        return View("Form", model);
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(45 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        ValidateProductForm(model, requireImages: false);
        if (!ModelState.IsValid)
        {
            await RestoreEditContextAsync(model, cancellationToken);
            return View("Form", model);
        }

        var result = await apiClient.UpdateProductAsync(model, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await RestoreEditContextAsync(model, cancellationToken);
            return View("Form", model);
        }
        TempData["Success"] = "Sản phẩm và tồn kho đã được cập nhật.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<ProductDto>($"api/products/{id}", cancellationToken: cancellationToken);
        if (!result.Success || result.Data is null) return NotFound();
        var actorId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedId) ? parsedId : (int?)null;
        var isOwner = actorId == result.Data.SellerId;
        BidListDto? bids = null;
        if (isOwner && User.HasClaim("verified", "true"))
        {
            var bidResult = await apiClient.GetAsync<BidListDto>($"api/products/{id}/bids", cancellationToken: cancellationToken);
            bids = bidResult.Data;
        }
        return View(new ProductDetailViewModel
        {
            Product = result.Data,
            Bids = bids,
            IsOwner = isOwner,
            BidAmount = (result.Data.HighestBid ?? result.Data.Price) + 1
        });
    }

    [Authorize]
    [HttpPost("{id:int}/bid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bid(int id, decimal bidAmount, CancellationToken cancellationToken)
    {
        var result = await apiClient.PostAsync<BidDto>($"api/products/{id}/bids", new { amount = bidAmount }, cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Giá của bạn đã được ghi nhận."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpPost("{id:int}/visibility")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisibility(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.PatchAsync<ProductDto>($"api/products/{id}/hide", cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? result.Data?.Status == "Hidden" ? "Đã tạm ẩn listing." : "Listing đã được hiển thị trở lại."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpPost("{id:int}/inventory")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Inventory(int id, int quantity, CancellationToken cancellationToken)
    {
        var result = await apiClient.PutAsync<InventoryDto>($"api/inventory/{id}", new { quantity }, cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Tồn kho đã được đồng bộ." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.DeleteAsync<object>($"api/products/{id}", cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Sản phẩm đã được xóa." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "VerifiedSeller")]
    [HttpPost("{id:int}/close-auction")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseAuction(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.PostAsync<AuctionCloseDto>($"api/products/{id}/close-auction", new { }, cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? result.Data?.WinnerId is null
                ? "Phiên đấu giá đã đóng và chưa có người thắng."
                : $"Đã chốt người thắng và tạo đơn hàng #{result.Data.OrderId}."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Detail), new { id });
    }

    private async Task PopulateCategoriesAsync(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<IReadOnlyList<CategoryDto>>("api/categories", false, cancellationToken);
        model.Categories = result.Data ?? [];
    }

    private async Task RestoreEditContextAsync(ProductFormViewModel model, CancellationToken cancellationToken)
    {
        await PopulateCategoriesAsync(model, cancellationToken);
        if (!model.Id.HasValue) return;
        var result = await apiClient.GetAsync<ProductDto>($"api/products/{model.Id.Value}", cancellationToken: cancellationToken);
        model.ExistingImages = result.Data?.Images ?? [];
    }

    private void ValidateProductForm(ProductFormViewModel model, bool requireImages)
    {
        if (requireImages && model.Images.Count == 0)
            ModelState.AddModelError(nameof(model.Images), "Vui lòng tải lên ít nhất một ảnh sản phẩm.");
        if (model.Images.Count > 8)
            ModelState.AddModelError(nameof(model.Images), "Mỗi sản phẩm có tối đa 8 ảnh.");
        if (model.IsAuction && (!model.AuctionEndTime.HasValue || model.AuctionEndTime.Value <= DateTime.Now.AddMinutes(1)))
            ModelState.AddModelError(nameof(model.AuctionEndTime), "Thời điểm kết thúc phải ở tương lai.");
    }

    private static ProductFormViewModel ToFormModel(ProductDto product) => new()
    {
        Id = product.Id,
        Title = product.Title,
        Description = product.Description,
        Price = product.Price,
        CategoryId = product.CategoryId,
        IsAuction = product.IsAuction,
        AuctionEndTime = product.AuctionEndTime?.ToLocalTime(),
        Quantity = product.Inventory.Quantity,
        ExistingImages = product.Images
    };
}
