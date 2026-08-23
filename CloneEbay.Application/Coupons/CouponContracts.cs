namespace CloneEbay.Application.Coupons;

public sealed record CouponResponse(
    int Id,
    string Code,
    decimal DiscountPercent,
    DateTime StartDate,
    DateTime EndDate,
    int MaxUsage,
    int UsageCount,
    int ProductId,
    string ProductTitle,
    int SellerId,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CouponListQuery(int? ProductId, string? Status);

public sealed record CreateCouponRequest(
    int ProductId,
    string Code,
    decimal DiscountPercent,
    DateTime StartDate,
    DateTime EndDate,
    int MaxUsage,
    string? RecaptchaToken);

public sealed record UpdateCouponRequest(
    string? Code,
    decimal? DiscountPercent,
    DateTime? StartDate,
    DateTime? EndDate,
    int? MaxUsage);

public sealed record CouponValidationResponse(
    bool IsValid,
    string? Reason,
    int? CouponId,
    decimal? DiscountPercent,
    int? RemainingUsage);
