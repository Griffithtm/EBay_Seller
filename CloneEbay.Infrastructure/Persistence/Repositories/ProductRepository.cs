using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(CloneEbayDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync(cancellationToken);

    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default) =>
        dbContext.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken);

    public async Task<Product?> GetByIdAsync(int productId, bool tracked, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = dbContext.Products
            .Include(product => product.Category)
            .Include(product => product.Seller)
            .Include(product => product.Inventory)
            .Include(product => product.Bids)
                .ThenInclude(bid => bid.Bidder);
        if (!tracked) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(product => product.Id == productId, cancellationToken);
    }

    public Task<Product?> GetByIdForUpdateAsync(int productId, CancellationToken cancellationToken = default) =>
        dbContext.Products
            .FromSqlInterpolated($"SELECT * FROM [dbo].[Product] WITH (UPDLOCK, ROWLOCK) WHERE [id] = {productId}")
            .Include(product => product.Category)
            .Include(product => product.Seller)
            .Include(product => product.Inventory)
            .Include(product => product.Bids)
                .ThenInclude(bid => bid.Bidder)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalItems)> ListAsync(
        int? sellerId,
        ProductStatus? status,
        int? categoryId,
        string? keyword,
        bool includeHidden,
        string? sortBy,
        bool descending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = dbContext.Products.AsNoTracking();
        if (sellerId.HasValue) query = query.Where(product => product.SellerId == sellerId.Value);
        if (status.HasValue) query = query.Where(product => product.Status == status.Value);
        else if (!includeHidden) query = query.Where(product => product.Status != ProductStatus.Hidden);
        if (categoryId.HasValue) query = query.Where(product => product.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword}%";
            query = query.Where(product => EF.Functions.Like(product.Title, pattern));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        query = (sortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("title", false) => query.OrderBy(product => product.Title),
            ("title", true) => query.OrderByDescending(product => product.Title),
            ("price", false) => query.OrderBy(product => product.Price),
            ("price", true) => query.OrderByDescending(product => product.Price),
            ("quantity", false) => query.OrderBy(product => product.Inventory.Quantity),
            ("quantity", true) => query.OrderByDescending(product => product.Inventory.Quantity),
            ("updatedat", false) => query.OrderBy(product => product.UpdatedAtUtc),
            _ => query.OrderByDescending(product => product.UpdatedAtUtc)
        };

        var items = await query
            .Include(product => product.Category)
            .Include(product => product.Seller)
            .Include(product => product.Inventory)
            .Include(product => product.Bids)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalItems);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        dbContext.Products.AddAsync(product, cancellationToken).AsTask();

    public void Remove(Product product) => dbContext.Products.Remove(product);

    public Task AddBidAsync(Bid bid, CancellationToken cancellationToken = default) =>
        dbContext.Bids.AddAsync(bid, cancellationToken).AsTask();

    public Task<bool> HasOrderItemsAsync(int productId, CancellationToken cancellationToken = default) =>
        dbContext.OrderItems.AnyAsync(item => item.ProductId == productId, cancellationToken);

    public Task<Order?> GetAuctionOrderAsync(int productId, CancellationToken cancellationToken = default) =>
        dbContext.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .SingleOrDefaultAsync(order => order.AuctionProductId == productId, cancellationToken);

    public Task AddOrderAsync(Order order, CancellationToken cancellationToken = default) =>
        dbContext.Orders.AddAsync(order, cancellationToken).AsTask();

    public async Task<IReadOnlyList<int>> GetExpiredAuctionIdsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default) =>
        await dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsAuction &&
                              product.AuctionEndTimeUtc <= utcNow &&
                              product.AuctionClosedAtUtc == null &&
                              product.Status != ProductStatus.AuctionEnded)
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);
}
