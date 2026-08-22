using CloneEbay.Application.Common;

namespace CloneEbay.Application.Products;

public sealed record CategoryResponse(int Id, string Name);

public sealed record InventoryResponse(int ProductId, int Quantity, DateTime LastUpdated);

public sealed record BidResponse(
    int Id,
    int BidderId,
    string BidderUsername,
    decimal Amount,
    DateTime BidTime);

public sealed record ProductResponse(
    int Id,
    string Title,
    string Description,
    decimal Price,
    IReadOnlyList<string> Images,
    int CategoryId,
    string CategoryName,
    int SellerId,
    string SellerUsername,
    string Status,
    bool IsAuction,
    DateTime? AuctionEndTime,
    DateTime? AuctionClosedAt,
    int? AuctionWinnerId,
    decimal? HighestBid,
    int BidCount,
    InventoryResponse Inventory,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ProductListQuery(
    int? SellerId,
    string? Status,
    int? CategoryId,
    string? Keyword,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortOrder = null);

public sealed record CreateProductRequest(
    string Title,
    string Description,
    decimal Price,
    int CategoryId,
    bool IsAuction,
    DateTime? AuctionEndTime,
    int Quantity,
    IReadOnlyList<string> Images);

public sealed record UpdateProductRequest(
    string? Title,
    string? Description,
    decimal? Price,
    int? CategoryId,
    bool? IsAuction,
    DateTime? AuctionEndTime,
    int? Quantity,
    IReadOnlyList<string>? Images);

public sealed record PlaceBidRequest(decimal Amount);

public sealed record BidListResponse(
    IReadOnlyList<BidResponse> Items,
    decimal? HighestBid,
    int TotalBids);

public sealed record AuctionCloseResponse(
    int ProductId,
    int? WinnerId,
    decimal? WinningAmount,
    int? OrderId,
    bool OrderCreationPending,
    DateTime ClosedAt);
