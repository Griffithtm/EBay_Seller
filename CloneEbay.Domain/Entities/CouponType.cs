using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class CouponType : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
}

/// <summary>Fixed ids for the 11 built-in coupon types, seeded by the coupon-upgrade migration.</summary>
public static class CouponTypeIds
{
    public const int ExtraPercentOff = 1;
    public const int ExtraPercentOffYOrMoreItems = 2;
    public const int ExtraAmountOffAmountOrMore = 3;
    public const int BuyXGetYAtPercentOff = 4;
    public const int BuyXGetYFree = 5;
    public const int ExtraPercentOffAmountOrMore = 6;
    public const int ExtraAmountOffItemsThreshold = 7;
    public const int ExtraAmountOff = 8;
    public const int ExtraAmountOffEachItem = 9;
    public const int SaveAmountForEveryItems = 10;
    public const int SaveAmountForEveryAmount = 11;
}
