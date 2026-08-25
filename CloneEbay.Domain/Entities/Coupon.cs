using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public int MaxUsage { get; set; }
    public int UsageCount { get; set; }
    public int ProductId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Product Product { get; set; } = null!;
}
