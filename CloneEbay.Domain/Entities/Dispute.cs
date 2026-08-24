using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Dispute : BaseEntity
{
    public int OrderId { get; set; }
    public int RaisedBy { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = DisputeStatus.Open;
    public string? Resolution { get; set; }

    public Order Order { get; set; } = null!;
    public User Raiser { get; set; } = null!;
}

public static class DisputeStatus
{
    public const string Open = "Open";
    public const string UnderReview = "UnderReview";
    public const string Resolved = "Resolved";
}
