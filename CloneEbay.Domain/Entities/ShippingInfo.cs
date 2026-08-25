using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class ShippingInfo : BaseEntity
{
    public int OrderId { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Preparing";
    public DateTime? EstimatedArrivalUtc { get; set; }

    public Order Order { get; set; } = null!;
}
