using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime? PaidAtUtc { get; set; }

    public Order Order { get; set; } = null!;
    public User User { get; set; } = null!;
}
