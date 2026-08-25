using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class DisputeEvidence : BaseEntity
{
    public int DisputeId { get; set; }
    public int UploaderId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Dispute Dispute { get; set; } = null!;
    public User Uploader { get; set; } = null!;
}
