using System.Text.Json;
using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Route("sale-events")]
[Authorize(Policy = "VerifiedSeller")]
public sealed class SaleEventsController(SellerApiClient apiClient) : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<IReadOnlyList<SaleEventSummaryDto>>("api/sale-events", cancellationToken: cancellationToken);
        if (!result.Success) TempData["Error"] = result.ErrorMessage;
        return View(new SaleEventIndexViewModel { Events = result.Data ?? [] });
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new SaleEventFormViewModel
        {
            StartDate = DateTime.Now.AddHours(1),
            EndDate = DateTime.Now.AddDays(7)
        };
        await PopulateEligibleProductsAsync(model, cancellationToken);
        return View("Form", model);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SaleEventFormViewModel model, CancellationToken cancellationToken)
    {
        var tiers = TryParseTiers(model.TiersJson);
        if (!ModelState.IsValid)
        {
            await PopulateEligibleProductsAsync(model, cancellationToken);
            return View("Form", model);
        }

        var result = await apiClient.PostAsync<SaleEventDetailDto>(
            "api/sale-events",
            BuildRequest(model, tiers),
            cancellationToken: cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await PopulateEligibleProductsAsync(model, cancellationToken);
            return View("Form", model);
        }
        TempData["Success"] = "Sự kiện giảm giá đã được tạo.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.GetAsync<SaleEventDetailDto>($"api/sale-events/{id}", cancellationToken: cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }
        var model = ToFormModel(result.Data);
        await PopulateEligibleProductsAsync(model, cancellationToken, excludeSaleEventId: id);
        return View("Form", model);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SaleEventFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        var tiers = TryParseTiers(model.TiersJson);
        if (!ModelState.IsValid)
        {
            await PopulateEligibleProductsAsync(model, cancellationToken, excludeSaleEventId: id);
            return View("Form", model);
        }

        var result = await apiClient.PutAsync<SaleEventDetailDto>(
            $"api/sale-events/{id}",
            BuildRequest(model, tiers),
            cancellationToken: cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            await PopulateEligibleProductsAsync(model, cancellationToken, excludeSaleEventId: id);
            return View("Form", model);
        }
        TempData["Success"] = "Sự kiện giảm giá đã được cập nhật.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, CancellationToken cancellationToken)
    {
        var result = await apiClient.PatchAsync<object>($"api/sale-events/{id}/status", new { status }, cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Trạng thái sự kiện đã được cập nhật." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.DeleteAsync<object>($"api/sale-events/{id}", cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Sự kiện đã được xóa." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateEligibleProductsAsync(SaleEventFormViewModel model, CancellationToken cancellationToken, int? excludeSaleEventId = null)
    {
        var query = "api/sale-events/eligible-products?pageSize=200&excludeAlreadyAssigned=true";
        if (excludeSaleEventId.HasValue) query += $"&excludeSaleEventId={excludeSaleEventId.Value}";
        var result = await apiClient.GetAsync<EligibleProductsPageDto>(query, cancellationToken: cancellationToken);
        var eligible = result.Data?.Items ?? [];
        // When editing, products already assigned to *this* event were excluded above, so add
        // them back in (marked as already-assigned) so the picker still shows them as checked.
        if (model.Id.HasValue)
        {
            var assignedIds = TryParseTiers(model.TiersJson)?.SelectMany(tier => tier.ProductIds).ToHashSet() ?? [];
            eligible = eligible.Concat(model.EligibleProducts.Where(product => assignedIds.Contains(product.ProductId) && eligible.All(item => item.ProductId != product.ProductId))).ToArray();
        }
        model.EligibleProducts = eligible;
    }

    private static object BuildRequest(SaleEventFormViewModel model, IReadOnlyList<TierJsonModel>? tiers) => new
    {
        name = model.Name,
        description = model.Description,
        mode = model.Mode,
        startDate = model.StartDate!.Value,
        endDate = model.EndDate!.Value,
        offerFreeShipping = model.OfferFreeShipping,
        includeSkippedItems = model.IncludeSkippedItems,
        blockPriceIncreaseRevisions = model.BlockPriceIncreaseRevisions,
        highlightPercentage = model.HighlightPercentage,
        tiers = model.Mode == "DiscountAndSaleEvent"
            ? tiers?.Select(tier => new
            {
                priority = tier.Priority,
                discountType = tier.DiscountType,
                discountValue = tier.DiscountValue,
                label = tier.Label,
                productIds = tier.ProductIds
            })
            : null
    };

    private IReadOnlyList<TierJsonModel>? TryParseTiers(string? tiersJson)
    {
        if (string.IsNullOrWhiteSpace(tiersJson)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<TierJsonModel>>(tiersJson, JsonOptions);
        }
        catch (JsonException)
        {
            ModelState.AddModelError(string.Empty, "Dữ liệu tier không hợp lệ.");
            return null;
        }
    }

    private static SaleEventFormViewModel ToFormModel(SaleEventDetailDto saleEvent) => new()
    {
        Id = saleEvent.Id,
        Name = saleEvent.Name,
        Description = saleEvent.Description,
        Mode = saleEvent.Mode,
        StartDate = saleEvent.StartDate.ToLocalTime(),
        EndDate = saleEvent.EndDate.ToLocalTime(),
        OfferFreeShipping = saleEvent.OfferFreeShipping,
        IncludeSkippedItems = saleEvent.IncludeSkippedItems,
        BlockPriceIncreaseRevisions = saleEvent.BlockPriceIncreaseRevisions,
        HighlightPercentage = saleEvent.HighlightPercentage,
        TiersJson = JsonSerializer.Serialize(saleEvent.Tiers.Select(tier => new TierJsonModel(
            tier.Priority, tier.DiscountType, tier.DiscountValue, tier.Label, tier.ProductIds.ToList())), JsonOptions)
    };

    private sealed record TierJsonModel(int Priority, string DiscountType, decimal DiscountValue, string? Label, List<int> ProductIds);
}
