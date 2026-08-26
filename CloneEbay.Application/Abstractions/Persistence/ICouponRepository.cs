using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface ICouponRepository
{
    Task<bool> CodeExistsAsync(string code, int? excludeCouponId, CancellationToken cancellationToken = default);
    Task<Coupon?> GetByIdAsync(int id, bool tracked, CancellationToken cancellationToken = default);
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Coupon>> ListForSellerAsync(int sellerId, CancellationToken cancellationToken = default);
    Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default);
    void Remove(Coupon coupon);
}

public interface ICouponTypeRepository
{
    Task<CouponType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CouponType>> ListAsync(CancellationToken cancellationToken = default);
}
