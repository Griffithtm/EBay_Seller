namespace CloneEbay.Application.Coupons;

public sealed record CouponConditionRequest(
    int? BuyQuantity,
    int? GetQuantity,
    decimal? GetDiscountPercent,
    decimal? SaveEveryAmount,
    int? SaveEveryItems,
    string? ConditionDescription);

public sealed record CreateCouponRequest(
    int CouponTypeId,
    string Name,
    string Code,
    decimal DiscountValue,
    string DiscountUnit,
    decimal? MaxDiscount,
    DateTime StartDate,
    DateTime EndDate,
    int? UsageLimit,
    int? UsagePerUser,
    decimal? MinimumOrderValue,
    decimal? ApplicablePriceMin,
    decimal? ApplicablePriceMax,
    int? CategoryId,
    int? ProductId,
    bool IsActive,
    CouponConditionRequest? Condition,
    string? RecaptchaToken);

public sealed record UpdateCouponRequest(
    string Name,
    decimal DiscountValue,
    string DiscountUnit,
    decimal? MaxDiscount,
    DateTime StartDate,
    DateTime EndDate,
    int? UsageLimit,
    int? UsagePerUser,
    decimal? MinimumOrderValue,
    decimal? ApplicablePriceMin,
    decimal? ApplicablePriceMax,
    int? CategoryId,
    int? ProductId,
    CouponConditionRequest? Condition);

public sealed record UpdateCouponStatusRequest(bool IsActive);

public sealed record CouponConditionResponse(
    int? BuyQuantity,
    int? GetQuantity,
    decimal? GetDiscountPercent,
    decimal? SaveEveryAmount,
    int? SaveEveryItems,
    string? ConditionDescription);

public sealed record CouponSummaryResponse(
    int Id,
    string Name,
    string Code,
    int CouponTypeId,
    string CouponTypeName,
    decimal DiscountValue,
    string DiscountUnit,
    decimal? MaxDiscount,
    DateTime StartDate,
    DateTime EndDate,
    bool IsActive,
    int? UsageLimit,
    int UsedCount);

public sealed record CouponDetailsResponse(
    int Id,
    string Name,
    string Code,
    int CouponTypeId,
    string CouponTypeName,
    int? SellerId,
    decimal DiscountValue,
    string DiscountUnit,
    decimal? MaxDiscount,
    DateTime StartDate,
    DateTime EndDate,
    int? UsageLimit,
    int? UsagePerUser,
    int UsedCount,
    decimal? MinimumOrderValue,
    decimal? ApplicablePriceMin,
    decimal? ApplicablePriceMax,
    int? CategoryId,
    int? ProductId,
    bool IsActive,
    CouponConditionResponse? Condition);

public sealed record CouponTypeResponse(int Id, string Name, string? Description, bool IsActive);

/// <summary>
/// Checkout-time lookup, kept for whatever builds the buyer-facing order flow next (mirrors the
/// old CouponService.ValidateAsync(code, productId) entry point). Returns whether the code can
/// currently be applied, independent of whether it actually gets redeemed.
/// </summary>
public sealed record ValidateCouponResponse(
    bool IsValid,
    string? Reason,
    int? CouponId,
    decimal? DiscountValue,
    string? DiscountUnit,
    decimal? MaxDiscount);
