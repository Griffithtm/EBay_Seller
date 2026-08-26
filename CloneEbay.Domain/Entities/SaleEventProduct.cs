using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class SaleEventProduct : BaseEntity
{
    public int SaleEventId { get; set; }
    public int TierId { get; set; }
    public int ProductId { get; set; }

    public SaleEvent SaleEvent { get; set; } = null!;
    public SaleEventTier Tier { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
