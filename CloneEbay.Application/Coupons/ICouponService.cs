namespace CloneEbay.Application.Coupons;

public interface ICouponService
{
    Task<IReadOnlyList<CouponTypeResponse>> GetTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CouponSummaryResponse>> GetForSellerAsync(int sellerId, CancellationToken cancellationToken = default);
    Task<CouponDetailsResponse> GetAsync(int couponId, int sellerId, CancellationToken cancellationToken = default);
    Task<CouponDetailsResponse> GetByCodeAsync(int actorId, string code, CancellationToken cancellationToken = default);
    Task<CouponDetailsResponse> CreateAsync(int sellerId, CreateCouponRequest request, CancellationToken cancellationToken = default);
    Task<CouponDetailsResponse> UpdateAsync(int couponId, int sellerId, UpdateCouponRequest request, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(int sellerId, int couponId, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteAsync(int sellerId, int couponId, CancellationToken cancellationToken = default);

    /// <summary>Checkout-time validity check; does not mark the coupon as used.</summary>
    Task<ValidateCouponResponse> ValidateAsync(string? code, int? productId, decimal? orderTotal, CancellationToken cancellationToken = default);
}
