using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Order : BaseEntity
{
    public int BuyerId { get; set; }
    public int? AddressId { get; set; }
    public DateTime OrderDateUtc { get; set; }
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "WaitingPayment";
    public int? AuctionProductId { get; set; }

    public User Buyer { get; set; } = null!;
    public Product? AuctionProduct { get; set; }
    public Address? Address { get; set; }
    public Payment? Payment { get; set; }
    public ShippingInfo? ShippingInfo { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
