using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.SaleEvents;

/// <summary>
/// Owns every Sale Event business rule (this project keeps entities as plain data holders and
/// puts all validation/state-machine logic in the service, matching ProductService's pattern).
/// Rules are adapted from the PRN232 EbayClone reference project's SaleEvent aggregate.
/// </summary>
public sealed class SaleEventService(
    ISaleEventRepository saleEvents,
    IUnitOfWork unitOfWork,
    IClock clock) : ISaleEventService
{
    private const int MaxNameLength = 90;
    private const int MaxTiers = 10;

    public async Task<IReadOnlyList<SaleEventSummaryResponse>> GetForSellerAsync(int sellerId, CancellationToken cancellationToken = default) =>
        (await saleEvents.ListForSellerAsync(sellerId, cancellationToken)).Select(MapSummary).ToArray();

    public async Task<SaleEventDetailResponse?> GetByIdAsync(int sellerId, int id, CancellationToken cancellationToken = default)
    {
        var saleEvent = await saleEvents.GetByIdAsync(sellerId, id, false, cancellationToken);
        return saleEvent is null ? null : MapDetail(saleEvent);
    }

    public async Task<SaleEventDetailResponse> CreateAsync(int sellerId, CreateSaleEventRequest request, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var mode = ParseMode(request.Mode);
        var name = ValidateName(request.Name);
        ValidateDateRange(request.StartDate, request.EndDate);

        if (await saleEvents.NameExistsAsync(sellerId, name.ToLowerInvariant(), null, cancellationToken))
            throw Problem(409, "NAME_ALREADY_EXISTS", "A sale event with this name already exists.");

        var saleEvent = new SaleEvent
        {
            SellerId = sellerId,
            Name = name,
            Description = CleanOptional(request.Description),
            Mode = mode,
            StartDateUtc = request.StartDate.ToUniversalTime(),
            EndDateUtc = request.EndDate.ToUniversalTime(),
            OfferFreeShipping = request.OfferFreeShipping,
            IncludeSkippedItems = request.IncludeSkippedItems,
            BlockPriceIncreaseRevisions = request.BlockPriceIncreaseRevisions,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        saleEvent.Status = DetermineInitialStatus(saleEvent.StartDateUtc, saleEvent.EndDateUtc, now);

        var (tiers, products, highlightPercentage) = await BuildModePayloadAsync(sellerId, mode, request.HighlightPercentage, request.Tiers, null, cancellationToken);
        saleEvent.HighlightPercentage = highlightPercentage;
        foreach (var tier in tiers) saleEvent.Tiers.Add(tier);
        foreach (var product in products) saleEvent.SaleEventProducts.Add(product);

        await saleEvents.AddAsync(saleEvent, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDetail(saleEvent);
    }

    public async Task<SaleEventDetailResponse> UpdateAsync(int sellerId, int id, UpdateSaleEventRequest request, CancellationToken cancellationToken = default)
    {
        var saleEvent = await RequiredOwnedAsync(sellerId, id, cancellationToken);
        if (saleEvent.Status is SaleEventStatus.Ended or SaleEventStatus.Cancelled)
            throw Problem(409, "EDIT_NOT_ALLOWED", "Ended or cancelled sale events can no longer be edited.");

        var now = clock.UtcNow;
        var mode = ParseMode(request.Mode);
        var name = ValidateName(request.Name);
        ValidateDateRange(request.StartDate, request.EndDate);

        if (await saleEvents.NameExistsAsync(sellerId, name.ToLowerInvariant(), id, cancellationToken))
            throw Problem(409, "NAME_ALREADY_EXISTS", "A sale event with this name already exists.");

        var (tiers, products, highlightPercentage) = await BuildModePayloadAsync(sellerId, mode, request.HighlightPercentage, request.Tiers, id, cancellationToken);

        saleEvent.Name = name;
        saleEvent.Description = CleanOptional(request.Description);
        saleEvent.Mode = mode;
        saleEvent.StartDateUtc = request.StartDate.ToUniversalTime();
        saleEvent.EndDateUtc = request.EndDate.ToUniversalTime();
        saleEvent.OfferFreeShipping = request.OfferFreeShipping;
        saleEvent.IncludeSkippedItems = request.IncludeSkippedItems;
        saleEvent.BlockPriceIncreaseRevisions = request.BlockPriceIncreaseRevisions;
        saleEvent.HighlightPercentage = highlightPercentage;
        saleEvent.Status = DetermineStatusAfterUpdate(saleEvent.Status, saleEvent.StartDateUtc, saleEvent.EndDateUtc, now);
        saleEvent.UpdatedAtUtc = now;

        saleEvents.ReplaceTiersAndProducts(saleEvent, tiers, products);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var reloaded = await saleEvents.GetByIdAsync(sellerId, id, false, cancellationToken)
            ?? throw Problem(404, "SALE_EVENT_NOT_FOUND", "Sale event was not found.");
        return MapDetail(reloaded);
    }

    public async Task UpdateStatusAsync(int sellerId, int id, string status, CancellationToken cancellationToken = default)
    {
        var saleEvent = await RequiredOwnedAsync(sellerId, id, cancellationToken);
        var newStatus = ParseStatus(status);
        if (newStatus == saleEvent.Status) return;

        if (saleEvent.Status is SaleEventStatus.Ended or SaleEventStatus.Cancelled)
            throw Problem(409, "INVALID_STATUS_TRANSITION", "Ended or cancelled sale events cannot change status.");
        if (!IsTransitionAllowed(saleEvent.Status, newStatus))
            throw Problem(409, "INVALID_STATUS_TRANSITION", $"Cannot move a sale event from {saleEvent.Status} to {newStatus}.");

        saleEvent.Status = newStatus;
        saleEvent.UpdatedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int sellerId, int id, CancellationToken cancellationToken = default)
    {
        var saleEvent = await RequiredOwnedAsync(sellerId, id, cancellationToken);
        saleEvents.Remove(saleEvent);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<EligibleProductsPageResponse> GetEligibleProductsAsync(
        int sellerId,
        int? excludeSaleEventId,
        EligibleProductQuery query,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(query.PageNumber, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var (items, totalCount) = await saleEvents.GetEligibleProductsAsync(
            sellerId,
            CleanOptional(query.SearchTerm),
            query.CategoryId,
            query.MinPrice,
            query.MaxPrice,
            excludeSaleEventId,
            query.ExcludeAlreadyAssigned,
            pageNumber,
            pageSize,
            cancellationToken);
        var responses = items.Select(product => new EligibleProductResponse(
            product.Id,
            product.Title,
            null,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.Price,
            product.CreatedAtUtc,
            false)).ToArray();
        return new EligibleProductsPageResponse(responses, totalCount, pageNumber, pageSize);
    }

    private async Task<SaleEvent> RequiredOwnedAsync(int sellerId, int id, CancellationToken cancellationToken) =>
        await saleEvents.GetByIdAsync(sellerId, id, true, cancellationToken)
        ?? throw Problem(404, "SALE_EVENT_NOT_FOUND", "Sale event was not found.");

    private async Task<(List<SaleEventTier> Tiers, List<SaleEventProduct> Products, decimal? HighlightPercentage)> BuildModePayloadAsync(
        int sellerId,
        SaleEventMode mode,
        decimal? highlightPercentage,
        IReadOnlyList<SaleEventTierRequest>? tierRequests,
        int? saleEventId,
        CancellationToken cancellationToken)
    {
        if (mode == SaleEventMode.SaleEventOnly)
        {
            if (tierRequests is { Count: > 0 })
                throw Problem(422, "INVALID_MODE_CONFIGURATION", "Highlight-only sale events cannot define discount tiers.");
            if (!highlightPercentage.HasValue)
                throw Problem(422, "HIGHLIGHT_PERCENTAGE_REQUIRED", "Highlight percentage is required for a highlight-only sale event.");
            if (highlightPercentage.Value is <= 0 or > 90)
                throw Problem(422, "INVALID_HIGHLIGHT_PERCENTAGE", "Highlight percentage must be between 1 and 90.");
            return ([], [], highlightPercentage.Value);
        }

        // DiscountAndSaleEvent
        if (tierRequests is null || tierRequests.Count == 0)
            throw Problem(422, "DISCOUNT_TIERS_REQUIRED", "At least one discount tier is required.");
        if (tierRequests.Count > MaxTiers)
            throw Problem(422, "TOO_MANY_DISCOUNT_TIERS", $"A sale event can define at most {MaxTiers} discount tiers.");

        var allProductIds = tierRequests.SelectMany(tier => tier.ProductIds).Distinct().ToArray();
        if (allProductIds.Length == 0)
            throw Problem(422, "LISTING_SELECTION_REQUIRED", "Each discount tier must include at least one product.");

        var ownedProductIds = await saleEvents.GetOwnedProductIdsAsync(sellerId, allProductIds, cancellationToken);
        if (ownedProductIds.Count != allProductIds.Length)
            throw Problem(403, "PRODUCTS_NOT_OWNED_BY_SELLER", "One or more selected products do not belong to this seller.");

        var tiers = new List<SaleEventTier>();
        var products = new List<SaleEventProduct>();
        var seenPriorities = new HashSet<int>();
        var assignedProductIds = new HashSet<int>();

        foreach (var tierRequest in tierRequests.OrderBy(tier => tier.Priority))
        {
            if (tierRequest.Priority <= 0)
                throw Problem(422, "INVALID_DISCOUNT_VALUE", "Tier priority must be greater than zero.");
            if (!seenPriorities.Add(tierRequest.Priority))
                throw Problem(409, "DUPLICATE_TIER_PRIORITY", $"Priority {tierRequest.Priority} is used by more than one tier.");

            var discountType = ParseDiscountType(tierRequest.DiscountType);
            if (discountType == SaleEventDiscountType.Percent && tierRequest.DiscountValue is <= 0 or > 90)
                throw Problem(422, "INVALID_DISCOUNT_PERCENTAGE", "Percentage discount tiers must be between 1 and 90.");
            if (discountType == SaleEventDiscountType.Amount && tierRequest.DiscountValue <= 0)
                throw Problem(422, "INVALID_DISCOUNT_VALUE", "Amount discount tiers must be greater than zero.");
            if (tierRequest.ProductIds.Count == 0)
                throw Problem(422, "LISTING_SELECTION_REQUIRED", "Each discount tier must include at least one product.");

            var tier = new SaleEventTier
            {
                Priority = tierRequest.Priority,
                DiscountType = discountType,
                DiscountValue = tierRequest.DiscountValue,
                Label = CleanOptional(tierRequest.Label)
            };
            tiers.Add(tier);

            foreach (var productId in tierRequest.ProductIds.Distinct())
            {
                if (!assignedProductIds.Add(productId))
                    throw Problem(409, "DUPLICATE_PRODUCT_ASSIGNMENT", "A product can only belong to one discount tier per sale event.");
                products.Add(new SaleEventProduct { Tier = tier, ProductId = productId });
            }
        }

        return (tiers, products, null);
    }

    private static SaleEventStatus DetermineInitialStatus(DateTime startUtc, DateTime endUtc, DateTime nowUtc) =>
        endUtc <= nowUtc ? SaleEventStatus.Ended
        : startUtc <= nowUtc ? SaleEventStatus.Active
        : SaleEventStatus.Scheduled;

    private static SaleEventStatus DetermineStatusAfterUpdate(SaleEventStatus current, DateTime startUtc, DateTime endUtc, DateTime nowUtc) =>
        current is SaleEventStatus.Cancelled or SaleEventStatus.Ended
            ? current
            : DetermineInitialStatus(startUtc, endUtc, nowUtc);

    private static bool IsTransitionAllowed(SaleEventStatus current, SaleEventStatus target) => current switch
    {
        SaleEventStatus.Draft => target is SaleEventStatus.Scheduled or SaleEventStatus.Active or SaleEventStatus.Cancelled,
        SaleEventStatus.Scheduled => target is SaleEventStatus.Draft or SaleEventStatus.Active or SaleEventStatus.Cancelled,
        SaleEventStatus.Active => target is SaleEventStatus.Ended or SaleEventStatus.Cancelled,
        _ => false
    };

    private static string ValidateName(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) throw Problem(422, "EMPTY_NAME", "Sale event name is required.");
        if (trimmed.Length > MaxNameLength) throw Problem(422, "NAME_TOO_LONG", $"Sale event name cannot exceed {MaxNameLength} characters.");
        return trimmed;
    }

    private static void ValidateDateRange(DateTime start, DateTime end)
    {
        if (end <= start) throw Problem(422, "INVALID_DATE_RANGE", "End date must be after start date.");
    }

    private static SaleEventMode ParseMode(string mode) =>
        Enum.TryParse<SaleEventMode>(mode, true, out var parsed)
            ? parsed
            : throw Problem(422, "INVALID_MODE_CONFIGURATION", "Sale event mode is invalid.");

    private static SaleEventStatus ParseStatus(string status) =>
        Enum.TryParse<SaleEventStatus>(status, true, out var parsed)
            ? parsed
            : throw Problem(422, "INVALID_STATUS_TRANSITION", "Sale event status is invalid.");

    private static SaleEventDiscountType ParseDiscountType(string discountType) =>
        Enum.TryParse<SaleEventDiscountType>(discountType, true, out var parsed)
            ? parsed
            : throw Problem(422, "INVALID_DISCOUNT_VALUE", "Discount type is invalid.");

    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static SaleEventTierResponse MapTier(SaleEventTier tier, IReadOnlyList<int> productIds) => new(
        tier.Id,
        tier.Priority,
        tier.DiscountType.ToString(),
        tier.DiscountValue,
        tier.Label,
        productIds);

    private static SaleEventDetailResponse MapDetail(SaleEvent saleEvent)
    {
        // Tier -> product ids is derived from saleEvent.SaleEventProducts (always Included by the
        // repository) rather than tier.SaleEventProducts, since that reverse navigation is not part
        // of the Include graph and would silently read back empty for AsNoTracking queries.
        var productIdsByTier = saleEvent.SaleEventProducts
            .GroupBy(item => item.TierId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<int>)group.Select(item => item.ProductId).ToArray());

        return new SaleEventDetailResponse(
            saleEvent.Id,
            saleEvent.Name,
            saleEvent.Description,
            saleEvent.Mode.ToString(),
            saleEvent.Status.ToString(),
            saleEvent.StartDateUtc,
            saleEvent.EndDateUtc,
            saleEvent.OfferFreeShipping,
            saleEvent.IncludeSkippedItems,
            saleEvent.BlockPriceIncreaseRevisions,
            saleEvent.HighlightPercentage,
            saleEvent.Tiers
                .OrderBy(tier => tier.Priority)
                .Select(tier => MapTier(tier, productIdsByTier.TryGetValue(tier.Id, out var ids) ? ids : []))
                .ToArray());
    }

    private static SaleEventSummaryResponse MapSummary(SaleEvent saleEvent) => new(
        saleEvent.Id,
        saleEvent.Name,
        saleEvent.Status.ToString(),
        saleEvent.StartDateUtc,
        saleEvent.EndDateUtc,
        saleEvent.Tiers.Count,
        saleEvent.SaleEventProducts.Count,
        saleEvent.OfferFreeShipping,
        saleEvent.IncludeSkippedItems,
        saleEvent.BlockPriceIncreaseRevisions,
        saleEvent.HighlightPercentage);

    private static AppProblemException Problem(int status, string code, string message) => new(status, code, message);
}
