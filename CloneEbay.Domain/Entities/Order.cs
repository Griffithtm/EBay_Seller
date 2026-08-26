using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Order : BaseEntity
{
    public int BuyerId { get; set; }
    public int? AddressId { get; set; }
    public DateTime OrderDateUtc { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "AWAITING_PAYMENT";
    public int? AuctionProductId { get; set; }
    public decimal FinalValueFee { get; set; }
    public string PayoutStatus { get; set; } = "Pending";

    public User Buyer { get; set; } = null!;
    public Product? AuctionProduct { get; set; }
    public Address? Address { get; set; }
    public Payment? Payment { get; set; }
    public ShippingInfo? ShippingInfo { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
