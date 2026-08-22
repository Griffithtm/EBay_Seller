using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Bid : BaseEntity
{
    public int ProductId { get; set; }
    public int BidderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime BidTimeUtc { get; set; }

    public Product Product { get; set; } = null!;
    public User Bidder { get; set; } = null!;
}
