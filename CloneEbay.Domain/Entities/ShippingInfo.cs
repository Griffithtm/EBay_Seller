using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class ShippingInfo : BaseEntity
{
    public int OrderId { get; set; }
    public string Carrier { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Preparing";
    public DateTime? EstimatedArrivalUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the order first left the seller (status moved to a
    /// "shipped" state). Drives the LateShipmentRate / TrackingUploadedOnTimeRate
    /// seller-performance metrics; null while the order has not shipped yet.
    /// </summary>
    public DateTime? ShippedAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}
