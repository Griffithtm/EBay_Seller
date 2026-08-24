using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Security;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Coupons;

public sealed class CouponService(
    ICouponRepository coupons,
    IUnitOfWork unitOfWork,
    IClock clock,
    IRecaptchaVerifier recaptchaVerifier) : ICouponService
{
    private const string CreateCouponAction = "create_coupon";

    public async Task<IReadOnlyList<CouponResponse>> ListAsync(
        int sellerId,
        CouponListQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = await coupons.ListAsync(sellerId, query.ProductId, cancellationToken);
        var now = clock.UtcNow;
        IEnumerable<CouponResponse> mapped = items.Select(coupon => Map(coupon, now));
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            mapped = mapped.Where(response => string.Equals(response.Status, query.Status, StringComparison.OrdinalIgnoreCase));
        }
        return mapped.ToArray();
    }

    public async Task<CouponResponse> GetAsync(int couponId, int sellerId, CancellationToken cancellationToken = default)
    {
        var coupon = await OwnedCouponAsync(couponId, sellerId, false, cancellationToken);
        return Map(coupon, clock.UtcNow);
    }

    public async Task<CouponResponse> CreateAsync(
        int sellerId,
        CreateCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        var recaptcha = await recaptchaVerifier.VerifyAsync(request.RecaptchaToken, CreateCouponAction, cancellationToken);
        if (!recaptcha.Success)
        {
            throw Problem(403, "RECAPTCHA_VERIFICATION_FAILED", "reCAPTCHA verification failed. Please retry the form.");
        }

        var code = NormalizeCode(request.Code);
        ValidateCore(code, request.DiscountPercent, request.StartDate, request.EndDate, request.MaxUsage);

        var productSellerId = await coupons.GetProductSellerIdAsync(request.ProductId, cancellationToken)
            ?? throw Problem(422, "PRODUCT_NOT_FOUND", "Selected product does not exist.");
        if (productSellerId != sellerId)
        {
            throw Problem(403, "FORBIDDEN_NOT_OWNER", "You can only create coupons for your own products.");
        }
        if (await coupons.CodeExistsForProductAsync(code, request.ProductId, null, cancellationToken))
        {
            throw Problem(409, "COUPON_CODE_ALREADY_EXISTS", "This coupon code already exists for the selected product.");
        }

        var now = clock.UtcNow;
        var coupon = new Coupon
        {
            ProductId = request.ProductId,
            Code = code,
            DiscountPercent = request.DiscountPercent,
            StartDateUtc = request.StartDate.ToUniversalTime(),
            EndDateUtc = request.EndDate.ToUniversalTime(),
            MaxUsage = request.MaxUsage,
            UsageCount = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await coupons.AddAsync(coupon, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        var saved = await coupons.GetByIdAsync(coupon.Id, false, cancellationToken) ?? coupon;
        return Map(saved, now);
    }

    public async Task<CouponResponse> UpdateAsync(
        int couponId,
        int sellerId,
        UpdateCouponRequest request,
        CancellationToken cancellationToken = default)
    {
        var coupon = await OwnedCouponAsync(couponId, sellerId, true, cancellationToken);
        var code = request.Code is null ? coupon.Code : NormalizeCode(request.Code);
        var discountPercent = request.DiscountPercent ?? coupon.DiscountPercent;
        var startDate = request.StartDate ?? coupon.StartDateUtc;
        var endDate = request.EndDate ?? coupon.EndDateUtc;
        var maxUsage = request.MaxUsage ?? coupon.MaxUsage;
        ValidateCore(code, discountPercent, startDate, endDate, maxUsage);
        if (maxUsage < coupon.UsageCount)
        {
            throw Problem(422, "COUPON_MAX_USAGE_BELOW_USED", "Max usage cannot be lower than the number of times already used.");
        }
        if (!string.Equals(code, coupon.Code, StringComparison.Ordinal) &&
            await coupons.CodeExistsForProductAsync(code, coupon.ProductId, coupon.Id, cancellationToken))
        {
            throw Problem(409, "COUPON_CODE_ALREADY_EXISTS", "This coupon code already exists for the selected product.");
        }

        coupon.Code = code;
        coupon.DiscountPercent = discountPercent;
        coupon.StartDateUtc = startDate.ToUniversalTime();
        coupon.EndDateUtc = endDate.ToUniversalTime();
        coupon.MaxUsage = maxUsage;
        coupon.UpdatedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(coupon, clock.UtcNow);
    }

    public async Task DeleteAsync(int couponId, int sellerId, CancellationToken cancellationToken = default)
    {
        var coupon = await OwnedCouponAsync(couponId, sellerId, true, cancellationToken);
        if (coupon.UsageCount > 0)
        {
            throw Problem(409, "COUPON_ALREADY_USED", "Coupons that have already been redeemed cannot be deleted.");
        }
        coupons.Remove(coupon);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CouponValidationResponse> ValidateAsync(
        string? code,
        int productId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return new CouponValidationResponse(false, "CODE_REQUIRED", null, null, null);
        }
        var coupon = await coupons.GetByCodeForProductAsync(NormalizeCode(code), productId, false, cancellationToken);
        if (coupon is null)
        {
            return new CouponValidationResponse(false, "COUPON_NOT_FOUND", null, null, null);
        }
        var now = clock.UtcNow;
        if (now < coupon.StartDateUtc)
        {
            return new CouponValidationResponse(false, "NOT_STARTED", coupon.Id, null, null);
        }
        if (now > coupon.EndDateUtc)
        {
            return new CouponValidationResponse(false, "EXPIRED", coupon.Id, null, null);
        }
        if (coupon.UsageCount >= coupon.MaxUsage)
        {
            return new CouponValidationResponse(false, "USAGE_LIMIT_REACHED", coupon.Id, null, null);
        }
        return new CouponValidationResponse(true, null, coupon.Id, coupon.DiscountPercent, coupon.MaxUsage - coupon.UsageCount);
    }

    private async Task<Coupon> OwnedCouponAsync(int couponId, int sellerId, bool tracked, CancellationToken cancellationToken)
    {
        var coupon = await coupons.GetByIdAsync(couponId, tracked, cancellationToken)
            ?? throw Problem(404, "COUPON_NOT_FOUND", "Coupon was not found.");
        return coupon.Product.SellerId == sellerId
            ? coupon
            : throw Problem(403, "FORBIDDEN_NOT_OWNER", "You can only manage coupons for your own products.");
    }

    private static void ValidateCore(string code, decimal discountPercent, DateTime startDate, DateTime endDate, int maxUsage)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length is < 3 or > 50)
            throw Problem(422, "COUPON_CODE_INVALID", "Coupon code must contain between 3 and 50 characters.");
        if (discountPercent is <= 0 or > 100)
            throw Problem(422, "COUPON_DISCOUNT_INVALID", "Discount percent must be greater than 0 and at most 100.");
        if (endDate <= startDate)
            throw Problem(422, "COUPON_DATE_RANGE_INVALID", "End date must be later than the start date.");
        if (maxUsage < 1)
            throw Problem(422, "COUPON_MAX_USAGE_INVALID", "Max usage must be at least 1.");
    }

    private static CouponResponse Map(Coupon coupon, DateTime now) => new(
        coupon.Id,
        coupon.Code,
        coupon.DiscountPercent,
        coupon.StartDateUtc,
        coupon.EndDateUtc,
        coupon.MaxUsage,
        coupon.UsageCount,
        coupon.ProductId,
        coupon.Product?.Title ?? string.Empty,
        coupon.Product?.SellerId ?? 0,
        ComputeStatus(coupon, now),
        coupon.CreatedAtUtc,
        coupon.UpdatedAtUtc);

    private static string ComputeStatus(Coupon coupon, DateTime now)
    {
        if (coupon.UsageCount >= coupon.MaxUsage) return "UsedUp";
        if (now < coupon.StartDateUtc) return "Scheduled";
        if (now > coupon.EndDateUtc) return "Expired";
        return "Active";
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    private static AppProblemException Problem(int status, string errorCode, string message) => new(status, errorCode, message);
}
