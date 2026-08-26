using CloneEbay.Application.Common;

namespace CloneEbay.Application.SaleEvents;

public sealed record SaleEventTierRequest(
    int Priority,
    string DiscountType,
    decimal DiscountValue,
    string? Label,
    IReadOnlyList<int> ProductIds);

public sealed record CreateSaleEventRequest(
    string Name,
    string? Description,
    string Mode,
    DateTime StartDate,
    DateTime EndDate,
    bool OfferFreeShipping,
    bool IncludeSkippedItems,
    bool BlockPriceIncreaseRevisions,
    decimal? HighlightPercentage,
    IReadOnlyList<SaleEventTierRequest>? Tiers);

public sealed record UpdateSaleEventRequest(
    string Name,
    string? Description,
    string Mode,
    DateTime StartDate,
    DateTime EndDate,
    bool OfferFreeShipping,
    bool IncludeSkippedItems,
    bool BlockPriceIncreaseRevisions,
    decimal? HighlightPercentage,
    IReadOnlyList<SaleEventTierRequest>? Tiers);

public sealed record UpdateSaleEventStatusRequest(string Status);

public sealed record SaleEventTierResponse(
    int Id,
    int Priority,
    string DiscountType,
    decimal DiscountValue,
    string? Label,
    IReadOnlyList<int> ProductIds);

public sealed record SaleEventDetailResponse(
    int Id,
    string Name,
    string? Description,
    string Mode,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    bool OfferFreeShipping,
    bool IncludeSkippedItems,
    bool BlockPriceIncreaseRevisions,
    decimal? HighlightPercentage,
    IReadOnlyList<SaleEventTierResponse> Tiers);

public sealed record SaleEventSummaryResponse(
    int Id,
    string Name,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    int TierCount,
    int ProductCount,
    bool OfferFreeShipping,
    bool IncludeSkippedItems,
    bool BlockPriceIncreaseRevisions,
    decimal? HighlightPercentage);

public sealed record EligibleProductQuery(
    string? SearchTerm,
    int? CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool ExcludeAlreadyAssigned = true,
    int PageNumber = 1,
    int PageSize = 25);

public sealed record EligibleProductResponse(
    int ProductId,
    string Title,
    string? ThumbnailUrl,
    int CategoryId,
    string CategoryName,
    decimal Price,
    DateTime CreatedAt,
    bool IsAlreadyAssigned);

public sealed record EligibleProductsPageResponse(
    IReadOnlyList<EligibleProductResponse> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
