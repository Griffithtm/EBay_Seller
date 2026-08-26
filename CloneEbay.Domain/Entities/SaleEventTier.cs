using CloneEbay.Domain.Common;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Domain.Entities;

public sealed class SaleEventTier : BaseEntity
{
    public int SaleEventId { get; set; }
    public SaleEventDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public int Priority { get; set; }
    public string? Label { get; set; }

    public SaleEvent SaleEvent { get; set; } = null!;
    public ICollection<SaleEventProduct> SaleEventProducts { get; set; } = new List<SaleEventProduct>();
}
