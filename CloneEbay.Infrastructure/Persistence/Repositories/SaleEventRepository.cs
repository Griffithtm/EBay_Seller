using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class SaleEventRepository(CloneEbayDbContext dbContext) : ISaleEventRepository
{
    public Task<bool> NameExistsAsync(int sellerId, string normalizedName, int? excludeId, CancellationToken cancellationToken = default) =>
        dbContext.SaleEvents.AnyAsync(
            saleEvent => saleEvent.SellerId == sellerId
                && saleEvent.Name.ToLower() == normalizedName
                && (!excludeId.HasValue || saleEvent.Id != excludeId.Value),
            cancellationToken);

    public async Task<IReadOnlyCollection<int>> GetOwnedProductIdsAsync(
        int sellerId,
        IReadOnlyCollection<int> productIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.Products
            .Where(product => product.SellerId == sellerId && productIds.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);

    public async Task<SaleEvent?> GetByIdAsync(int sellerId, int id, bool tracked, CancellationToken cancellationToken = default)
    {
        IQueryable<SaleEvent> query = dbContext.SaleEvents
            .Include(saleEvent => saleEvent.Tiers)
            .Include(saleEvent => saleEvent.SaleEventProducts)
                .ThenInclude(item => item.Product);
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(
            saleEvent => saleEvent.Id == id && saleEvent.SellerId == sellerId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<SaleEvent>> ListForSellerAsync(int sellerId, CancellationToken cancellationToken = default) =>
        await dbContext.SaleEvents
            .AsNoTracking()
            .Where(saleEvent => saleEvent.SellerId == sellerId)
            .Include(saleEvent => saleEvent.Tiers)
            .Include(saleEvent => saleEvent.SaleEventProducts)
            .OrderByDescending(saleEvent => saleEvent.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task AddAsync(SaleEvent saleEvent, CancellationToken cancellationToken = default) =>
        dbContext.SaleEvents.AddAsync(saleEvent, cancellationToken).AsTask();

    public void Remove(SaleEvent saleEvent) => dbContext.SaleEvents.Remove(saleEvent);

    public void ReplaceTiersAndProducts(SaleEvent saleEvent, IReadOnlyList<SaleEventTier> newTiers, IReadOnlyList<SaleEventProduct> newProducts)
    {
        // Materialize before removing: RemoveRange triggers EF's delete-fixup, which mutates
        // these exact navigation collections as a side effect — iterating the live collection
        // while that happens throws. Adding the replacements through the (now-tracked) parent's
        // navigation, rather than AddRange on the DbSet directly, is what lets EF resolve the
        // required saleEventId FK automatically once SaveChanges assigns the parent's real id.
        var existingProducts = saleEvent.SaleEventProducts.ToList();
        var existingTiers = saleEvent.Tiers.ToList();
        if (existingProducts.Count > 0) dbContext.SaleEventProducts.RemoveRange(existingProducts);
        if (existingTiers.Count > 0) dbContext.SaleEventTiers.RemoveRange(existingTiers);

        foreach (var tier in newTiers) saleEvent.Tiers.Add(tier);
        foreach (var product in newProducts) saleEvent.SaleEventProducts.Add(product);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalItems)> GetEligibleProductsAsync(
        int sellerId,
        string? searchTerm,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        int? excludeSaleEventId,
        bool excludeAlreadyAssigned,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = dbContext.Products
            .AsNoTracking()
            .Where(product => product.SellerId == sellerId && product.Status != ProductStatus.Hidden);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var pattern = $"%{searchTerm}%";
            query = query.Where(product => EF.Functions.Like(product.Title, pattern));
        }
        if (categoryId.HasValue) query = query.Where(product => product.CategoryId == categoryId.Value);
        if (minPrice.HasValue) query = query.Where(product => product.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(product => product.Price <= maxPrice.Value);
        if (excludeAlreadyAssigned)
        {
            query = query.Where(product => !dbContext.SaleEventProducts.Any(item =>
                item.ProductId == product.Id
                && (!excludeSaleEventId.HasValue || item.SaleEventId != excludeSaleEventId.Value)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(product => product.Category)
            .OrderByDescending(product => product.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalItems);
    }
}
