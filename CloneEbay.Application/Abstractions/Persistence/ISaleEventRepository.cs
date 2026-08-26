using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface ISaleEventRepository
{
    Task<bool> NameExistsAsync(int sellerId, string normalizedName, int? excludeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<int>> GetOwnedProductIdsAsync(int sellerId, IReadOnlyCollection<int> productIds, CancellationToken cancellationToken = default);
    Task<SaleEvent?> GetByIdAsync(int sellerId, int id, bool tracked, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SaleEvent>> ListForSellerAsync(int sellerId, CancellationToken cancellationToken = default);
    Task AddAsync(SaleEvent saleEvent, CancellationToken cancellationToken = default);
    void Remove(SaleEvent saleEvent);
    void ReplaceTiersAndProducts(SaleEvent saleEvent, IReadOnlyList<SaleEventTier> newTiers, IReadOnlyList<SaleEventProduct> newProducts);
    Task<(IReadOnlyList<Product> Items, int TotalItems)> GetEligibleProductsAsync(
        int sellerId,
        string? searchTerm,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int? excludeSaleEventId,
        bool excludeAlreadyAssigned,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
