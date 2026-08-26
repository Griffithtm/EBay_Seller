using System.Text.Json;
using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Application.Sellers;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Products;

public sealed class ProductService(
    IProductRepository products,
    IUnitOfWork unitOfWork,
    IClock clock,
    ISellerPerformanceService sellerPerformance) : IProductService
{
    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        (await products.GetCategoriesAsync(cancellationToken))
            .Select(category => new CategoryResponse(category.Id, category.Name))
            .ToArray();

    public async Task<PagedResult<ProductResponse>> ListAsync(
        ProductListQuery query,
        bool includeHidden,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        ProductStatus? status = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<ProductStatus>(query.Status, true, out var parsedStatus))
                throw Problem(400, "INVALID_PRODUCT_STATUS", "Product status is invalid.");
            status = parsedStatus;
        }

        var (items, totalItems) = await products.ListAsync(
            query.SellerId,
            status,
            query.CategoryId,
            CleanOptional(query.Keyword),
            includeHidden,
            query.SortBy,
            string.Equals(query.SortOrder, "desc", StringComparison.OrdinalIgnoreCase),
            page,
            pageSize,
            cancellationToken);
        return new PagedResult<ProductResponse>(items.Select(Map).ToArray(), page, pageSize, totalItems);
    }

    public async Task<ProductResponse> GetAsync(
        int productId,
        int? actorUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var product = await RequiredProductAsync(productId, false, cancellationToken);
        if (product.Status == ProductStatus.Hidden && product.SellerId != actorUserId && !isAdmin)
        {
            throw Problem(404, "PRODUCT_NOT_FOUND", "Product was not found.");
        }
        return Map(product);
    }

    public async Task<ProductResponse> CreateAsync(
        int sellerId,
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        ValidateCore(request.Title, request.Description, request.Price, request.CategoryId);
        ValidateAuction(request.IsAuction, request.AuctionEndTime, now);
        if (request.Quantity < 0)
        {
            throw Problem(422, "QUANTITY_NEGATIVE", "Inventory quantity cannot be negative.");
        }
        if (request.Images.Count is < 1 or > 8)
        {
            throw Problem(422, "PRODUCT_IMAGES_INVALID", "Provide between 1 and 8 product images.");
        }
        if (!await products.CategoryExistsAsync(request.CategoryId, cancellationToken))
        {
            throw Problem(422, "CATEGORY_NOT_FOUND", "Selected category does not exist.");
        }

        // Seller performance selling-limit gate: throws AppProblemException(403) if the
        // seller's tier has no room left for one more active listing of this value.
        var estimatedValue = request.IsAuction ? request.Price : request.Price * request.Quantity;
        await sellerPerformance.EnsureCanListAsync(sellerId, estimatedValue, cancellationToken);

        var product = new Product
        {
            SellerId = sellerId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Price = request.Price,
            CategoryId = request.CategoryId,
            IsAuction = request.IsAuction,
            AuctionEndTimeUtc = request.IsAuction ? request.AuctionEndTime?.ToUniversalTime() : null,
            ImagesJson = SerializeImages(request.Images),
            Status = request.Quantity == 0 ? ProductStatus.OutOfStock : ProductStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Inventory = new Inventory { Quantity = request.Quantity, LastUpdatedUtc = now }
        };
        await products.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(await RequiredProductAsync(product.Id, false, cancellationToken));
    }

    public async Task<ProductResponse> UpdateAsync(
        int productId,
        int sellerId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginSerializableTransactionAsync(cancellationToken);
        var product = await LockedOwnedProductAsync(productId, sellerId, cancellationToken);
        if (product.Status == ProductStatus.AuctionEnded)
        {
            throw Problem(409, "AUCTION_ALREADY_CLOSED", "An ended auction cannot be modified.");
        }

        var title = request.Title is null ? product.Title : request.Title.Trim();
        var description = request.Description is null ? product.Description : request.Description.Trim();
        var price = request.Price ?? product.Price;
        var categoryId = request.CategoryId ?? product.CategoryId;
        var isAuction = request.IsAuction ?? product.IsAuction;
        var auctionEnd = request.AuctionEndTime ?? product.AuctionEndTimeUtc;
        ValidateCore(title, description, price, categoryId);
        ValidateAuction(isAuction, auctionEnd, clock.UtcNow, allowExistingPastEnd: true);
        if (!await products.CategoryExistsAsync(categoryId, cancellationToken))
        {
            throw Problem(422, "CATEGORY_NOT_FOUND", "Selected category does not exist.");
        }
        if (request.Images is { Count: > 0 } && request.Images.Count > 8)
        {
            throw Problem(422, "PRODUCT_IMAGES_INVALID", "A product can have at most 8 images.");
        }
        if (request.Quantity is < 0)
        {
            throw Problem(422, "QUANTITY_NEGATIVE", "Inventory quantity cannot be negative.");
        }

        product.Title = title;
        product.Description = description;
        product.Price = price;
        product.CategoryId = categoryId;
        product.IsAuction = isAuction;
        product.AuctionEndTimeUtc = isAuction ? auctionEnd?.ToUniversalTime() : null;
        if (request.Images is { Count: > 0 }) product.ImagesJson = SerializeImages(request.Images);
        if (request.Quantity.HasValue)
        {
            product.Inventory.Quantity = request.Quantity.Value;
            product.Inventory.LastUpdatedUtc = clock.UtcNow;
            if (request.Quantity.Value == 0 && product.Status == ProductStatus.Active) product.Status = ProductStatus.OutOfStock;
            if (request.Quantity.Value > 0 && product.Status == ProductStatus.OutOfStock) product.Status = ProductStatus.Active;
        }
        product.UpdatedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(product);
    }

    public async Task<ProductResponse> ToggleVisibilityAsync(
        int productId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginSerializableTransactionAsync(cancellationToken);
        var product = await LockedOwnedProductAsync(productId, sellerId, cancellationToken);
        if (product.Status == ProductStatus.AuctionEnded)
        {
            throw Problem(409, "AUCTION_ALREADY_CLOSED", "A closed auction cannot be hidden or reopened.");
        }
        product.Status = product.Status == ProductStatus.Hidden
            ? product.Inventory.Quantity == 0 ? ProductStatus.OutOfStock : ProductStatus.Active
            : ProductStatus.Hidden;
        product.UpdatedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(product);
    }

    public async Task DeleteAsync(int productId, int sellerId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginSerializableTransactionAsync(cancellationToken);
        var product = await LockedOwnedProductAsync(productId, sellerId, cancellationToken);
        if (await products.HasOrderItemsAsync(productId, cancellationToken))
        {
            throw Problem(409, "PRODUCT_HAS_ORDERS", "Products referenced by an order cannot be deleted. Hide the listing instead.");
        }
        if (product.Bids.Count > 0)
        {
            throw Problem(409, "PRODUCT_HAS_BIDS", "Products with bids cannot be deleted. Hide the listing instead.");
        }
        products.Remove(product);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<InventoryResponse> GetInventoryAsync(
        int productId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var product = await OwnedProductAsync(productId, sellerId, cancellationToken);
        return MapInventory(product.Inventory);
    }

    public async Task<InventoryResponse> UpdateInventoryAsync(
        int productId,
        int sellerId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity < 0) throw Problem(422, "QUANTITY_NEGATIVE", "Inventory quantity cannot be negative.");
        await using var transaction = await unitOfWork.BeginSerializableTransactionAsync(cancellationToken);
        var product = await LockedOwnedProductAsync(productId, sellerId, cancellationToken);
        if (product.Status == ProductStatus.AuctionEnded)
        {
            throw Problem(409, "AUCTION_ALREADY_CLOSED", "Inventory cannot change after an auction closes.");
        }
        product.Inventory.Quantity = quantity;
        product.Inventory.LastUpdatedUtc = clock.UtcNow;
        if (quantity == 0 && product.Status == ProductStatus.Active) product.Status = ProductStatus.OutOfStock;
        if (quantity > 0 && product.Status == ProductStatus.OutOfStock) product.Status = ProductStatus.Active;
        product.UpdatedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return MapInventory(product.Inventory);
    }

    public async Task<BidResponse> PlaceBidAsync(
        int productId,
        int bidderId,
        PlaceBidRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginSerializableTransactionAsync(cancellationToken);
        var product = await RequiredLockedProductAsync(productId, cancellationToken);
        var now = clock.UtcNow;
        if (!product.IsAuction) throw Problem(422, "NOT_AN_AUCTION_PRODUCT", "This product is not an auction listing.");
        if (product.SellerId == bidderId) throw Problem(403, "SELLER_CANNOT_BID_OWN_PRODUCT", "Sellers cannot bid on their own product.");
        if (product.Status != ProductStatus.Active || product.AuctionClosedAtUtc.HasValue || product.AuctionEndTimeUtc <= now)
        {
            throw Problem(409, "AUCTION_ALREADY_CLOSED", "This auction is no longer accepting bids.");
        }
        var minimum = product.Bids.Count == 0 ? product.Price : product.Bids.Max(bid => bid.Amount) + 1m;
        if (request.Amount < minimum)
        {
            throw Problem(422, "BID_TOO_LOW", $"Bid amount must be at least {minimum:0.##}.");
        }
        var bid = new Bid { ProductId = product.Id, BidderId = bidderId, Amount = request.Amount, BidTimeUtc = now };
        await products.AddBidAsync(bid, cancellationToken);
        product.UpdatedAtUtc = now;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new BidResponse(bid.Id, bidderId, string.Empty, bid.Amount, bid.BidTimeUtc);
    }

    public async Task<BidListResponse> GetBidsAsync(
        int productId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var product = await OwnedProductAsync(productId, sellerId, cancellationToken);
        if (!product.IsAuction) throw Problem(422, "NOT_AN_AUCTION_PRODUCT", "This product is not an auction listing.");
        var items = product.Bids
            .OrderByDescending(bid => bid.Amount)
            .ThenBy(bid => bid.BidTimeUtc)
            .Select(MapBid)
            .ToArray();
        return new BidListResponse(items, items.FirstOrDefault()?.Amount, items.Length);
    }

    public async Task<AuctionCloseResponse> CloseAuctionAsync(
        int productId,
        int? sellerId,
        bool systemOperation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await unitOfWork.BeginSerializableTransactionAsync(cancellationToken);
        var product = await RequiredLockedProductAsync(productId, cancellationToken);
        if (!product.IsAuction) throw Problem(422, "NOT_AN_AUCTION_PRODUCT", "This product is not an auction listing.");
        if (!systemOperation && product.SellerId != sellerId) throw Problem(403, "FORBIDDEN_NOT_OWNER", "Only the listing owner can close this auction.");
        if (product.AuctionClosedAtUtc.HasValue)
        {
            var existingOrder = await products.GetAuctionOrderAsync(product.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return MapClosedAuction(product, existingOrder);
        }
        if (!systemOperation && product.AuctionEndTimeUtc > clock.UtcNow) throw Problem(409, "AUCTION_NOT_ENDED", "The auction cannot close before its end time.");

        var result = await CloseCoreAsync(product, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<int> CloseExpiredAuctionsAsync(CancellationToken cancellationToken = default)
    {
        var expiredIds = await products.GetExpiredAuctionIdsAsync(clock.UtcNow, cancellationToken);
        foreach (var productId in expiredIds)
        {
            await CloseAuctionAsync(productId, null, true, cancellationToken);
        }
        return expiredIds.Count;
    }

    private async Task<AuctionCloseResponse> CloseCoreAsync(Product product, CancellationToken cancellationToken)
    {
        var winner = product.Bids.OrderByDescending(bid => bid.Amount).ThenBy(bid => bid.BidTimeUtc).FirstOrDefault();
        var now = clock.UtcNow;
        product.AuctionWinnerId = winner?.BidderId;
        product.AuctionClosedAtUtc = now;
        product.Status = ProductStatus.AuctionEnded;
        product.UpdatedAtUtc = now;
        product.Inventory.Quantity = 0;
        product.Inventory.LastUpdatedUtc = now;
        Order? order = null;
        if (winner is not null)
        {
            order = new Order
            {
                BuyerId = winner.BidderId,
                OrderDateUtc = now,
                TotalPrice = winner.Amount,
                Status = "WaitingPayment",
                AuctionProductId = product.Id,
                Items =
                [
                    new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = 1,
                        UnitPrice = winner.Amount
                    }
                ]
            };
            await products.AddOrderAsync(order, cancellationToken);
        }
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new AuctionCloseResponse(product.Id, winner?.BidderId, winner?.Amount, order?.Id, false, now);
    }

    private async Task<Product> OwnedProductAsync(int productId, int sellerId, CancellationToken cancellationToken)
    {
        var product = await RequiredProductAsync(productId, true, cancellationToken);
        return product.SellerId == sellerId
            ? product
            : throw Problem(403, "FORBIDDEN_NOT_OWNER", "You can only manage your own products.");
    }

    private async Task<Product> LockedOwnedProductAsync(int productId, int sellerId, CancellationToken cancellationToken)
    {
        var product = await RequiredLockedProductAsync(productId, cancellationToken);
        return product.SellerId == sellerId
            ? product
            : throw Problem(403, "FORBIDDEN_NOT_OWNER", "You can only manage your own products.");
    }

    private async Task<Product> RequiredLockedProductAsync(int productId, CancellationToken cancellationToken) =>
        await products.GetByIdForUpdateAsync(productId, cancellationToken)
        ?? throw Problem(404, "PRODUCT_NOT_FOUND", "Product was not found.");

    private static AuctionCloseResponse MapClosedAuction(Product product, Order? order)
    {
        var winningBid = product.Bids
            .OrderByDescending(bid => bid.Amount)
            .ThenBy(bid => bid.BidTimeUtc)
            .FirstOrDefault();
        return new AuctionCloseResponse(
            product.Id,
            product.AuctionWinnerId,
            winningBid?.Amount,
            order?.Id,
            false,
            product.AuctionClosedAtUtc!.Value);
    }

    private async Task<Product> RequiredProductAsync(int productId, bool tracked, CancellationToken cancellationToken) =>
        await products.GetByIdAsync(productId, tracked, cancellationToken)
        ?? throw Problem(404, "PRODUCT_NOT_FOUND", "Product was not found.");

    private static void ValidateCore(string title, string description, decimal price, int categoryId)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length is < 3 or > 255)
            throw Problem(422, "PRODUCT_TITLE_INVALID", "Product title must contain between 3 and 255 characters.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 4000)
            throw Problem(422, "PRODUCT_DESCRIPTION_INVALID", "Product description is required and cannot exceed 4,000 characters.");
        if (price <= 0) throw Problem(422, "PRODUCT_PRICE_INVALID", "Product price must be greater than zero.");
        if (categoryId <= 0) throw Problem(422, "CATEGORY_NOT_FOUND", "Select a valid category.");
    }

    private static void ValidateAuction(bool isAuction, DateTime? endTime, DateTime utcNow, bool allowExistingPastEnd = false)
    {
        if (!isAuction) return;
        if (!endTime.HasValue || (!allowExistingPastEnd && endTime.Value.ToUniversalTime() <= utcNow.AddMinutes(1)))
            throw Problem(422, "AUCTION_END_TIME_REQUIRED", "Auction end time must be at least one minute in the future.");
    }

    private static ProductResponse Map(Product product)
    {
        var highestBid = product.Bids.Count == 0 ? null : (decimal?)product.Bids.Max(bid => bid.Amount);
        return new ProductResponse(
            product.Id,
            product.Title,
            product.Description,
            product.Price,
            DeserializeImages(product.ImagesJson),
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.SellerId,
            product.Seller?.Username ?? string.Empty,
            product.Status.ToString(),
            product.IsAuction,
            product.AuctionEndTimeUtc,
            product.AuctionClosedAtUtc,
            product.AuctionWinnerId,
            highestBid,
            product.Bids.Count,
            MapInventory(product.Inventory),
            product.CreatedAtUtc,
            product.UpdatedAtUtc);
    }

    private static BidResponse MapBid(Bid bid) =>
        new(bid.Id, bid.BidderId, bid.Bidder?.Username ?? string.Empty, bid.Amount, bid.BidTimeUtc);

    private static InventoryResponse MapInventory(Inventory inventory) =>
        new(inventory.ProductId, inventory.Quantity, inventory.LastUpdatedUtc);

    private static IReadOnlyList<string> DeserializeImages(string json)
    {
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    private static string SerializeImages(IReadOnlyList<string> images) => JsonSerializer.Serialize(images);
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static AppProblemException Problem(int status, string code, string message) => new(status, code, message);
}
