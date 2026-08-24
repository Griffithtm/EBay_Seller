using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface ICouponRepository
{
    Task<Coupon?> GetByIdAsync(int couponId, bool tracked, CancellationToken cancellationToken = default);
    Task<Coupon?> GetByCodeForProductAsync(string code, int productId, bool tracked, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsForProductAsync(string code, int productId, int? excludeCouponId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Coupon>> ListAsync(int? sellerId, int? productId, CancellationToken cancellationToken = default);
    Task<int?> GetProductSellerIdAsync(int productId, CancellationToken cancellationToken = default);
    Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default);
    void Remove(Coupon coupon);
}
