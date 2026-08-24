using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Feedback : BaseEntity
{
    public int SellerId { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public decimal PositiveRate { get; set; }

    public User Seller { get; set; } = null!;
}
