using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Review : BaseEntity
{
    public int ProductId { get; set; }
    public int ReviewerId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
}
