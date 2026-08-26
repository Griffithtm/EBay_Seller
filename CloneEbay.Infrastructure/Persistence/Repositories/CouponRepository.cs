using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloneEbay.Infrastructure.Persistence.Repositories;

public sealed class CouponRepository(CloneEbayDbContext dbContext) : ICouponRepository
{
    public Task<bool> CodeExistsAsync(string code, int? excludeCouponId, CancellationToken cancellationToken = default) =>
        dbContext.Coupons.AnyAsync(
            coupon => coupon.Code == code && coupon.Id != (excludeCouponId ?? 0),
            cancellationToken);

    public Task<Coupon?> GetByIdAsync(int id, bool tracked, CancellationToken cancellationToken = default)
    {
        IQueryable<Coupon> query = dbContext.Coupons
            .Include(coupon => coupon.CouponType)
            .Include(coupon => coupon.Conditions);
        if (!tracked) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(coupon => coupon.Id == id, cancellationToken);
    }

    public Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        dbContext.Coupons
            .AsNoTracking()
            .Include(coupon => coupon.CouponType)
            .Include(coupon => coupon.Conditions)
            .SingleOrDefaultAsync(coupon => coupon.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Coupon>> ListForSellerAsync(int sellerId, CancellationToken cancellationToken = default) =>
        await dbContext.Coupons
            .AsNoTracking()
            .Include(coupon => coupon.CouponType)
            .Where(coupon => coupon.SellerId == sellerId)
            .OrderByDescending(coupon => coupon.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default) =>
        dbContext.Coupons.AddAsync(coupon, cancellationToken).AsTask();

    public void Remove(Coupon coupon) => dbContext.Coupons.Remove(coupon);
}
