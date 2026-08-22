using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class StoreRepository(CloneEbayDbContext dbContext) : IStoreRepository
{
    public Task<Store?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Stores.Include(store => store.Seller).SingleOrDefaultAsync(store => store.Id == id, cancellationToken);

    public Task<Store?> GetBySellerIdAsync(int sellerId, CancellationToken cancellationToken = default) =>
        dbContext.Stores.Include(store => store.Seller).SingleOrDefaultAsync(store => store.SellerId == sellerId, cancellationToken);

    public Task<bool> StoreNameExistsAsync(string storeName, int? excludingStoreId, CancellationToken cancellationToken = default) =>
        dbContext.Stores.AnyAsync(
            store => store.StoreName == storeName && (!excludingStoreId.HasValue || store.Id != excludingStoreId.Value),
            cancellationToken);

    public async Task<PagedResult<Store>> ListApplicationsAsync(
        VerificationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Stores.Include(store => store.Seller).AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(store => store.VerificationStatus == status.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(store => store.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Store>(items, page, pageSize, total);
    }

    public async Task AddAsync(Store store, CancellationToken cancellationToken = default) =>
        await dbContext.Stores.AddAsync(store, cancellationToken);
}
