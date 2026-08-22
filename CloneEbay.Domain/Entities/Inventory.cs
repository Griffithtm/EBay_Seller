using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Inventory : BaseEntity
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime LastUpdatedUtc { get; set; }

    public Product Product { get; set; } = null!;
}
