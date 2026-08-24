namespace CloneEbay.Application.Coupons;

public interface ICouponService
{
    Task<IReadOnlyList<CouponResponse>> ListAsync(int sellerId, CouponListQuery query, CancellationToken cancellationToken = default);
    Task<CouponResponse> GetAsync(int couponId, int sellerId, CancellationToken cancellationToken = default);
    Task<CouponResponse> CreateAsync(int sellerId, CreateCouponRequest request, CancellationToken cancellationToken = default);
    Task<CouponResponse> UpdateAsync(int couponId, int sellerId, UpdateCouponRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int couponId, int sellerId, CancellationToken cancellationToken = default);
    Task<CouponValidationResponse> ValidateAsync(string? code, int productId, CancellationToken cancellationToken = default);
}
