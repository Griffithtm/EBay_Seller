using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class CouponRepository(CloneEbayDbContext dbContext) : ICouponRepository
{
    public Task<Coupon?> GetByIdAsync(int couponId, bool tracked, CancellationToken cancellationToken = default)
    {
        IQueryable<Coupon> query = dbContext.Coupons.Include(coupon => coupon.Product);
        if (!tracked) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(coupon => coupon.Id == couponId, cancellationToken);
    }

    public Task<Coupon?> GetByCodeForProductAsync(string code, int productId, bool tracked, CancellationToken cancellationToken = default)
    {
        IQueryable<Coupon> query = dbContext.Coupons.Include(coupon => coupon.Product);
        if (!tracked) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(coupon => coupon.ProductId == productId && coupon.Code == code, cancellationToken);
    }

    public Task<bool> CodeExistsForProductAsync(string code, int productId, int? excludeCouponId, CancellationToken cancellationToken = default) =>
        dbContext.Coupons.AnyAsync(
            coupon => coupon.ProductId == productId && coupon.Code == code && coupon.Id != (excludeCouponId ?? 0),
            cancellationToken);

    public async Task<IReadOnlyList<Coupon>> ListAsync(int? sellerId, int? productId, CancellationToken cancellationToken = default)
    {
        IQueryable<Coupon> query = dbContext.Coupons.AsNoTracking().Include(coupon => coupon.Product);
        if (sellerId.HasValue) query = query.Where(coupon => coupon.Product.SellerId == sellerId.Value);
        if (productId.HasValue) query = query.Where(coupon => coupon.ProductId == productId.Value);
        return await query.OrderByDescending(coupon => coupon.CreatedAtUtc).ToListAsync(cancellationToken);
    }

    public Task<int?> GetProductSellerIdAsync(int productId, CancellationToken cancellationToken = default) =>
        dbContext.Products.Where(product => product.Id == productId).Select(product => (int?)product.SellerId).SingleOrDefaultAsync(cancellationToken);

    public Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default) =>
        dbContext.Coupons.AddAsync(coupon, cancellationToken).AsTask();

    public void Remove(Coupon coupon) => dbContext.Coupons.Remove(coupon);
}
