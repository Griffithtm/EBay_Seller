using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class CouponCondition : BaseEntity
{
    public int CouponId { get; set; }
    public int? BuyQuantity { get; set; }
    public int? GetQuantity { get; set; }
    public decimal? GetDiscountPercent { get; set; }
    public decimal? SaveEveryAmount { get; set; }
    public int? SaveEveryItems { get; set; }
    public string? ConditionDescription { get; set; }

    public Coupon Coupon { get; set; } = null!;
}
