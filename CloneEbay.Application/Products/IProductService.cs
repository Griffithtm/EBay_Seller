using CloneEbay.Application.Common;

namespace CloneEbay.Application.Products;

public interface IProductService
{
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<ProductResponse>> ListAsync(ProductListQuery query, bool includeHidden, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetAsync(int productId, int? actorUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateAsync(int sellerId, CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateAsync(int productId, int sellerId, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> ToggleVisibilityAsync(int productId, int sellerId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int productId, int sellerId, CancellationToken cancellationToken = default);
    Task<InventoryResponse> GetInventoryAsync(int productId, int sellerId, CancellationToken cancellationToken = default);
    Task<InventoryResponse> UpdateInventoryAsync(int productId, int sellerId, int quantity, CancellationToken cancellationToken = default);
    Task<BidResponse> PlaceBidAsync(int productId, int bidderId, PlaceBidRequest request, CancellationToken cancellationToken = default);
    Task<BidListResponse> GetBidsAsync(int productId, int sellerId, CancellationToken cancellationToken = default);
    Task<AuctionCloseResponse> CloseAuctionAsync(int productId, int? sellerId, bool systemOperation, CancellationToken cancellationToken = default);
    Task<int> CloseExpiredAuctionsAsync(CancellationToken cancellationToken = default);
}
