using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Security;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;
using System.Text.RegularExpressions;

namespace CloneEbay.Application.Coupons;

/// <summary>
/// Owns every Coupon business rule, including the per-coupon-type condition table (11 built-in
/// types). Upgraded from the original one-product/one-percent coupon design — see
/// UpgradeCouponToTypedSellerModel migration and Coupon.cs for what was kept for backward
/// compatibility.
/// </summary>
public sealed partial class CouponService(
    ICouponRepository coupons,
    ICouponTypeRepository couponTypes,
    IUnitOfWork unitOfWork,
    IClock clock,
    IRecaptchaVerifier recaptchaVerifier) : ICouponService
{
    private const string CreateCouponAction = "create_coupon";

    public async Task<IReadOnlyList<CouponTypeResponse>> GetTypesAsync(CancellationToken cancellationToken = default) =>
        (await couponTypes.ListAsync(cancellationToken))
            .Select(type => new CouponTypeResponse(type.Id, type.Name, type.Description, type.IsActive))
            .ToArray();

    public async Task<IReadOnlyList<CouponSummaryResponse>> GetForSellerAsync(int sellerId, CancellationToken cancellationToken = default) =>
        (await coupons.ListForSellerAsync(sellerId, cancellationToken)).Select(MapSummary).ToArray();

    public async Task<CouponDetailsResponse> GetAsync(int couponId, int sellerId, CancellationToken cancellationToken = default) =>
        MapDetails(await RequiredOwnedAsync(sellerId, couponId, tracked: false, cancellationToken));

    public async Task<CouponDetailsResponse> GetByCodeAsync(int actorId, string code, CancellationToken cancellationToken = default)
    {
        var trimmed = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (trimmed.Length == 0) throw Problem(422, "COUPON_CODE_INVALID", "Coupon code is required.");

        var coupon = await coupons.GetByCodeAsync(trimmed, cancellationToken)
            ?? throw Problem(404, "COUPON_NOT_FOUND", "Coupon was not found.");
        if (coupon.SellerId.HasValue && coupon.SellerId.Value != actorId)
            throw Problem(403, "FORBIDDEN_NOT_OWNER", "This coupon belongs to another seller.");
        return MapDetails(coupon);
    }

    public async Task<CouponDetailsResponse> CreateAsync(int sellerId, CreateCouponRequest request, CancellationToken cancellationToken = default)
    {
        var recaptcha = await recaptchaVerifier.VerifyAsync(request.RecaptchaToken, CreateCouponAction, cancellationToken);
        if (!recaptcha.Success)
            throw Problem(403, "RECAPTCHA_VERIFICATION_FAILED", "reCAPTCHA verification failed. Please retry the form.");

        var now = clock.UtcNow;
        var (name, code, discountUnit) = ValidateCore(request.Name, request.Code, request.DiscountValue, request.DiscountUnit,
            request.MaxDiscount, request.StartDate, request.EndDate, request.UsageLimit, request.UsagePerUser,
            request.MinimumOrderValue, request.ApplicablePriceMin, request.ApplicablePriceMax);

        var couponType = await couponTypes.GetByIdAsync(request.CouponTypeId, cancellationToken)
            ?? throw Problem(404, "COUPON_TYPE_NOT_FOUND", "Coupon type was not found.");
        if (!couponType.IsActive) throw Problem(409, "COUPON_TYPE_INACTIVE", "This coupon type is no longer available.");

        var (effectiveUnit, effectiveMinOrder, condition) = ValidateTypeSpecificData(
            request.CouponTypeId, discountUnit, request.MinimumOrderValue, request.Condition);

        if (await coupons.CodeExistsAsync(code, null, cancellationToken))
            throw Problem(409, "COUPON_CODE_ALREADY_EXISTS", "This coupon code is already in use.");

        var coupon = new Coupon
        {
            CouponTypeId = request.CouponTypeId,
            CategoryId = request.CategoryId,
            SellerId = sellerId,
            ProductId = request.ProductId,
            Name = name,
            Code = code,
            DiscountValue = request.DiscountValue,
            DiscountUnit = effectiveUnit,
            MaxDiscount = request.MaxDiscount,
            StartDate = request.StartDate.ToUniversalTime(),
            EndDate = request.EndDate.ToUniversalTime(),
            UsageLimit = request.UsageLimit,
            UsagePerUser = request.UsagePerUser,
            UsedCount = 0,
            MinimumOrderValue = effectiveMinOrder,
            ApplicablePriceMin = request.ApplicablePriceMin,
            ApplicablePriceMax = request.ApplicablePriceMax,
            IsActive = request.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        if (condition is not null) coupon.Conditions.Add(condition);

        await coupons.AddAsync(coupon, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var reloaded = await coupons.GetByIdAsync(coupon.Id, false, cancellationToken)
            ?? throw Problem(404, "COUPON_NOT_FOUND", "Coupon was not found.");
        return MapDetails(reloaded);
    }

    public async Task<CouponDetailsResponse> UpdateAsync(int couponId, int sellerId, UpdateCouponRequest request, CancellationToken cancellationToken = default)
    {
        var coupon = await RequiredOwnedAsync(sellerId, couponId, tracked: true, cancellationToken);

        var (name, code, discountUnit) = ValidateCore(request.Name, coupon.Code, request.DiscountValue, request.DiscountUnit,
            request.MaxDiscount, request.StartDate, request.EndDate, request.UsageLimit, request.UsagePerUser,
            request.MinimumOrderValue, request.ApplicablePriceMin, request.ApplicablePriceMax);

        // The coupon type is fixed at creation time — changing it would require re-validating a
        // completely different condition shape against data the seller didn't just submit.
        var (effectiveUnit, effectiveMinOrder, condition) = ValidateTypeSpecificData(
            coupon.CouponTypeId, discountUnit, request.MinimumOrderValue, request.Condition);

        coupon.Name = name;
        coupon.DiscountValue = request.DiscountValue;
        coupon.DiscountUnit = effectiveUnit;
        coupon.MaxDiscount = request.MaxDiscount;
        coupon.StartDate = request.StartDate.ToUniversalTime();
        coupon.EndDate = request.EndDate.ToUniversalTime();
        coupon.UsageLimit = request.UsageLimit;
        coupon.UsagePerUser = request.UsagePerUser;
        coupon.MinimumOrderValue = effectiveMinOrder;
        coupon.ApplicablePriceMin = request.ApplicablePriceMin;
        coupon.ApplicablePriceMax = request.ApplicablePriceMax;
        coupon.CategoryId = request.CategoryId;
        coupon.ProductId = request.ProductId;
        coupon.UpdatedAtUtc = clock.UtcNow;

        coupon.Conditions.Clear();
        if (condition is not null) coupon.Conditions.Add(condition);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var reloaded = await coupons.GetByIdAsync(couponId, false, cancellationToken)
            ?? throw Problem(404, "COUPON_NOT_FOUND", "Coupon was not found.");
        return MapDetails(reloaded);
    }

    public async Task UpdateStatusAsync(int sellerId, int couponId, bool isActive, CancellationToken cancellationToken = default)
    {
        var coupon = await RequiredOwnedAsync(sellerId, couponId, tracked: true, cancellationToken);
        coupon.IsActive = isActive;
        coupon.UpdatedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int sellerId, int couponId, CancellationToken cancellationToken = default)
    {
        var coupon = await RequiredOwnedAsync(sellerId, couponId, tracked: true, cancellationToken);
        if (coupon.UsedCount > 0)
            throw Problem(409, "COUPON_ALREADY_USED", "This coupon has already been redeemed and cannot be deleted.");
        coupons.Remove(coupon);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ValidateCouponResponse> ValidateAsync(string? code, int? productId, decimal? orderTotal, CancellationToken cancellationToken = default)
    {
        var trimmed = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (trimmed.Length == 0) return new ValidateCouponResponse(false, "EMPTY_CODE", null, null, null, null);

        var coupon = await coupons.GetByCodeAsync(trimmed, cancellationToken);
        if (coupon is null) return new ValidateCouponResponse(false, "NOT_FOUND", null, null, null, null);
        if (!coupon.IsActive) return new ValidateCouponResponse(false, "INACTIVE", coupon.Id, null, null, null);

        var now = clock.UtcNow;
        if (now < coupon.StartDate || now > coupon.EndDate)
            return new ValidateCouponResponse(false, "OUT_OF_DATE_RANGE", coupon.Id, null, null, null);
        if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            return new ValidateCouponResponse(false, "USAGE_LIMIT_REACHED", coupon.Id, null, null, null);
        if (coupon.ProductId.HasValue && productId.HasValue && coupon.ProductId.Value != productId.Value)
            return new ValidateCouponResponse(false, "NOT_APPLICABLE_TO_PRODUCT", coupon.Id, null, null, null);
        if (coupon.MinimumOrderValue.HasValue && orderTotal.HasValue && orderTotal.Value < coupon.MinimumOrderValue.Value)
            return new ValidateCouponResponse(false, "MINIMUM_ORDER_NOT_MET", coupon.Id, null, null, null);

        return new ValidateCouponResponse(true, null, coupon.Id, coupon.DiscountValue, coupon.DiscountUnit.ToString(), coupon.MaxDiscount);
    }

    private async Task<Coupon> RequiredOwnedAsync(int sellerId, int couponId, bool tracked, CancellationToken cancellationToken)
    {
        var coupon = await coupons.GetByIdAsync(couponId, tracked, cancellationToken)
            ?? throw Problem(404, "COUPON_NOT_FOUND", "Coupon was not found.");
        // A coupon with no SellerId is a platform/global coupon: intentionally unmanageable by
        // any seller through these endpoints.
        if (coupon.SellerId != sellerId) throw Problem(403, "FORBIDDEN_NOT_OWNER", "You can only manage your own coupons.");
        return coupon;
    }

    private static (string Name, string Code, CouponDiscountUnit DiscountUnit) ValidateCore(
        string name,
        string code,
        decimal discountValue,
        string discountUnit,
        decimal? maxDiscount,
        DateTime startDate,
        DateTime endDate,
        int? usageLimit,
        int? usagePerUser,
        decimal? minimumOrderValue,
        decimal? applicablePriceMin,
        decimal? applicablePriceMax)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0 || trimmedName.Length > 100)
            throw Problem(422, "COUPON_CODE_INVALID", "Coupon name must be between 1 and 100 characters.");

        var normalizedCode = (code?.Trim() ?? string.Empty).ToUpperInvariant();
        if (normalizedCode.Length == 0 || normalizedCode.Length > 50 || !CodePattern().IsMatch(normalizedCode))
            throw Problem(422, "COUPON_CODE_INVALID", "Coupon code must contain only letters, digits and hyphens.");

        if (endDate <= startDate) throw Problem(422, "COUPON_DATE_RANGE_INVALID", "End date must be after start date.");
        if (discountValue <= 0) throw Problem(422, "COUPON_DISCOUNT_INVALID", "Discount value must be greater than zero.");
        if (maxDiscount is <= 0) throw Problem(422, "COUPON_DISCOUNT_INVALID", "Maximum discount must be greater than zero.");
        if (usageLimit is <= 0) throw Problem(422, "COUPON_MAX_USAGE_INVALID", "Usage limit must be greater than zero.");
        if (usagePerUser is <= 0) throw Problem(422, "COUPON_MAX_USAGE_INVALID", "Per-user usage limit must be greater than zero.");
        if (usageLimit.HasValue && usagePerUser.HasValue && usagePerUser.Value > usageLimit.Value)
            throw Problem(422, "COUPON_MAX_USAGE_INVALID", "Per-user usage limit cannot exceed the total usage limit.");
        if (minimumOrderValue is < 0) throw Problem(422, "COUPON_DISCOUNT_INVALID", "Minimum order value cannot be negative.");
        if (applicablePriceMin is < 0 || applicablePriceMax is < 0)
            throw Problem(422, "COUPON_DISCOUNT_INVALID", "Applicable price bounds cannot be negative.");
        if (applicablePriceMin.HasValue && applicablePriceMax.HasValue && applicablePriceMin.Value > applicablePriceMax.Value)
            throw Problem(422, "COUPON_DISCOUNT_INVALID", "Minimum applicable price cannot exceed the maximum.");

        var unit = Enum.TryParse<CouponDiscountUnit>(discountUnit, true, out var parsedUnit)
            ? parsedUnit
            : throw Problem(422, "COUPON_DISCOUNT_INVALID", "Discount unit is invalid.");

        return (trimmedName, normalizedCode, unit);
    }

    /// <summary>
    /// Applies the fixed rule table for each of the 11 built-in coupon types: which discount unit
    /// it requires, whether a minimum order value is mandatory, and whether exactly zero or one
    /// condition record is required/forbidden — and what shape that condition must take.
    /// </summary>
    private static (CouponDiscountUnit Unit, decimal? MinimumOrderValue, CouponCondition? Condition) ValidateTypeSpecificData(
        int couponTypeId,
        CouponDiscountUnit requestedUnit,
        decimal? minimumOrderValue,
        CouponConditionRequest? conditionRequest)
    {
        CouponDiscountUnit RequireUnit(CouponDiscountUnit required)
        {
            if (requestedUnit != required)
                throw Problem(422, "COUPON_DISCOUNT_INVALID", $"This coupon type requires the {required} discount unit.");
            return required;
        }

        void ForbidConditions()
        {
            if (conditionRequest is not null)
                throw Problem(422, "COUPON_DISCOUNT_INVALID", "This coupon type does not accept a condition.");
        }

        CouponCondition RequireCondition(Func<CouponConditionRequest, CouponCondition> build)
        {
            if (conditionRequest is null)
                throw Problem(422, "COUPON_DISCOUNT_INVALID", "This coupon type requires exactly one condition.");
            return build(conditionRequest);
        }

        decimal RequirePositiveMinimumOrderValue()
        {
            if (minimumOrderValue is not > 0)
                throw Problem(422, "COUPON_DISCOUNT_INVALID", "This coupon type requires a minimum order value greater than zero.");
            return minimumOrderValue.Value;
        }

        switch (couponTypeId)
        {
            case CouponTypeIds.ExtraPercentOff:
                ForbidConditions();
                return (RequireUnit(CouponDiscountUnit.Percent), minimumOrderValue, null);

            case CouponTypeIds.ExtraPercentOffYOrMoreItems:
                return (RequireUnit(CouponDiscountUnit.Percent), minimumOrderValue, RequireCondition(source =>
                    source.SaveEveryItems is > 0
                        ? BuildCondition(source)
                        : throw Problem(422, "COUPON_DISCOUNT_INVALID", "SaveEveryItems must be greater than zero.")));

            case CouponTypeIds.ExtraAmountOffAmountOrMore:
                ForbidConditions();
                return (RequireUnit(CouponDiscountUnit.Amount), RequirePositiveMinimumOrderValue(), null);

            case CouponTypeIds.BuyXGetYAtPercentOff:
                return (RequireUnit(CouponDiscountUnit.Percent), minimumOrderValue, RequireCondition(source =>
                    source is { BuyQuantity: > 0, GetQuantity: > 0, GetDiscountPercent: > 0 }
                        ? BuildCondition(source)
                        : throw Problem(422, "COUPON_DISCOUNT_INVALID", "BuyQuantity, GetQuantity and GetDiscountPercent must all be greater than zero.")));

            case CouponTypeIds.BuyXGetYFree:
                return (requestedUnit, minimumOrderValue, RequireCondition(source =>
                    source is { BuyQuantity: > 0, GetQuantity: > 0 }
                        ? BuildCondition(source with { GetDiscountPercent = 100m })
                        : throw Problem(422, "COUPON_DISCOUNT_INVALID", "BuyQuantity and GetQuantity must both be greater than zero.")));

            case CouponTypeIds.ExtraPercentOffAmountOrMore:
                ForbidConditions();
                return (RequireUnit(CouponDiscountUnit.Percent), RequirePositiveMinimumOrderValue(), null);

            case CouponTypeIds.ExtraAmountOffItemsThreshold:
                return (RequireUnit(CouponDiscountUnit.Amount), minimumOrderValue, RequireCondition(source =>
                    source.SaveEveryItems is > 0
                        ? BuildCondition(source)
                        : throw Problem(422, "COUPON_DISCOUNT_INVALID", "SaveEveryItems must be greater than zero.")));

            case CouponTypeIds.ExtraAmountOff:
            case CouponTypeIds.ExtraAmountOffEachItem:
                ForbidConditions();
                return (RequireUnit(CouponDiscountUnit.Amount), minimumOrderValue, null);

            case CouponTypeIds.SaveAmountForEveryItems:
                return (RequireUnit(CouponDiscountUnit.Amount), minimumOrderValue, RequireCondition(source =>
                    source is { SaveEveryAmount: > 0, SaveEveryItems: > 0 }
                        ? BuildCondition(source)
                        : throw Problem(422, "COUPON_DISCOUNT_INVALID", "SaveEveryAmount and SaveEveryItems must both be greater than zero.")));

            case CouponTypeIds.SaveAmountForEveryAmount:
                return (RequireUnit(CouponDiscountUnit.Amount), minimumOrderValue, RequireCondition(source =>
                    source.SaveEveryAmount is > 0
                        ? BuildCondition(source)
                        : throw Problem(422, "COUPON_DISCOUNT_INVALID", "SaveEveryAmount must be greater than zero.")));

            default:
                throw Problem(422, "COUPON_DISCOUNT_INVALID", "Unrecognized coupon type.");
        }
    }

    private static CouponCondition BuildCondition(CouponConditionRequest source) => new()
    {
        BuyQuantity = source.BuyQuantity,
        GetQuantity = source.GetQuantity,
        GetDiscountPercent = source.GetDiscountPercent,
        SaveEveryAmount = source.SaveEveryAmount,
        SaveEveryItems = source.SaveEveryItems,
        ConditionDescription = string.IsNullOrWhiteSpace(source.ConditionDescription) ? null : source.ConditionDescription.Trim()
    };

    private static CouponConditionResponse? MapCondition(CouponCondition? condition) =>
        condition is null ? null : new CouponConditionResponse(
            condition.BuyQuantity,
            condition.GetQuantity,
            condition.GetDiscountPercent,
            condition.SaveEveryAmount,
            condition.SaveEveryItems,
            condition.ConditionDescription);

    private static CouponSummaryResponse MapSummary(Coupon coupon) => new(
        coupon.Id,
        coupon.Name,
        coupon.Code,
        coupon.CouponTypeId,
        coupon.CouponType?.Name ?? string.Empty,
        coupon.DiscountValue,
        coupon.DiscountUnit.ToString(),
        coupon.MaxDiscount,
        coupon.StartDate,
        coupon.EndDate,
        coupon.IsActive,
        coupon.UsageLimit,
        coupon.UsedCount);

    private static CouponDetailsResponse MapDetails(Coupon coupon) => new(
        coupon.Id,
        coupon.Name,
        coupon.Code,
        coupon.CouponTypeId,
        coupon.CouponType?.Name ?? string.Empty,
        coupon.SellerId,
        coupon.DiscountValue,
        coupon.DiscountUnit.ToString(),
        coupon.MaxDiscount,
        coupon.StartDate,
        coupon.EndDate,
        coupon.UsageLimit,
        coupon.UsagePerUser,
        coupon.UsedCount,
        coupon.MinimumOrderValue,
        coupon.ApplicablePriceMin,
        coupon.ApplicablePriceMax,
        coupon.CategoryId,
        coupon.ProductId,
        coupon.IsActive,
        MapCondition(coupon.Conditions.FirstOrDefault()));

    private static AppProblemException Problem(int status, string code, string message) => new(status, code, message);

    [GeneratedRegex("^[A-Z0-9-]+$")]
    private static partial Regex CodePattern();
}
