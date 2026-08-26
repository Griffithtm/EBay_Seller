using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Message : BaseEntity
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public int? RelatedProductId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }

    // Navigation properties
    public User Sender { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
