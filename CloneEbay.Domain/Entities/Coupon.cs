using CloneEbay.Domain.Common;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Domain.Entities;

/// <summary>
/// Upgraded from the original per-product "Role 3 Coupon" design (single discount percent tied
/// to one product) to a full, seller-owned, typed coupon system. [code], [discountPercent] and
/// [productId] are kept as physical columns for backward compatibility with pre-existing rows —
/// DiscountPercent is populated only for coupons created under the old model; new coupons use
/// DiscountValue + DiscountUnit instead. ProductId is now optional (a coupon no longer has to be
/// scoped to a single product).
/// </summary>
public sealed class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public decimal? DiscountPercent { get; set; }
    public int? ProductId { get; set; }

    public int CouponTypeId { get; set; }
    public int? CategoryId { get; set; }
    public int? SellerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DiscountValue { get; set; }
    public CouponDiscountUnit DiscountUnit { get; set; }
    public decimal? MaxDiscount { get; set; }
    public int? UsageLimit { get; set; }
    public int? UsagePerUser { get; set; }
    public int UsedCount { get; set; }
    public decimal? MinimumOrderValue { get; set; }
    public decimal? ApplicablePriceMin { get; set; }
    public decimal? ApplicablePriceMax { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public CouponType CouponType { get; set; } = null!;
    public User? Seller { get; set; }
    public Category? Category { get; set; }
    public Product? Product { get; set; }
    public ICollection<CouponCondition> Conditions { get; set; } = new List<CouponCondition>();
}
