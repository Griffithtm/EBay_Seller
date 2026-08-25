using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class DisputeMessage : BaseEntity
{
    public int DisputeId { get; set; }
    public int SenderId { get; set; }
    public string SenderRole { get; set; } = string.Empty; // "Buyer", "Seller", "Admin"
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Dispute Dispute { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
