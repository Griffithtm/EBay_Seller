using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int productId, bool tracked, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdForUpdateAsync(int productId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Product> Items, int TotalItems)> ListAsync(
        int? sellerId,
        ProductStatus? status,
        int? categoryId,
        string? keyword,
        bool includeHidden,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    void Remove(Product product);
    Task AddBidAsync(Bid bid, CancellationToken cancellationToken = default);
    Task<bool> HasOrderItemsAsync(int productId, CancellationToken cancellationToken = default);
    Task<Order?> GetAuctionOrderAsync(int productId, CancellationToken cancellationToken = default);
    Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<int>> GetExpiredAuctionIdsAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
