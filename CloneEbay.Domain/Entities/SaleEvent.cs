using CloneEbay.Domain.Common;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Domain.Entities;

public sealed class SaleEvent : BaseEntity
{
    public int SellerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SaleEventMode Mode { get; set; }
    public SaleEventStatus Status { get; set; } = SaleEventStatus.Draft;
    public DateTime StartDateUtc { get; set; }
    public DateTime EndDateUtc { get; set; }
    public bool OfferFreeShipping { get; set; }
    public bool IncludeSkippedItems { get; set; }
    public bool BlockPriceIncreaseRevisions { get; set; }
    public decimal? HighlightPercentage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User Seller { get; set; } = null!;
    public ICollection<SaleEventTier> Tiers { get; set; } = new List<SaleEventTier>();
    public ICollection<SaleEventProduct> SaleEventProducts { get; set; } = new List<SaleEventProduct>();
}
