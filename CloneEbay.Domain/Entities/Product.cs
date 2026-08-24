using CloneEbay.Domain.Common;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Domain.Entities;

public sealed class Product : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImagesJson { get; set; } = "[]";
    public int CategoryId { get; set; }
    public int SellerId { get; set; }
    public bool IsAuction { get; set; }
    public DateTime? AuctionEndTimeUtc { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? AuctionClosedAtUtc { get; set; }
    public int? AuctionWinnerId { get; set; }

    public Category Category { get; set; } = null!;
    public User Seller { get; set; } = null!;
    public User? AuctionWinner { get; set; }
    public Inventory Inventory { get; set; } = null!;
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    public Order? AuctionOrder { get; set; }
}
