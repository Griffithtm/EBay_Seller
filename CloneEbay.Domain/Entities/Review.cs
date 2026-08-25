using CloneEbay.Domain.Common;

namespace CloneEbay.Domain.Entities;

public sealed class Review : BaseEntity
{
    public int ProductId { get; set; }
    public int ReviewerId { get; set; }
    public int? RecipientId { get; set; }
    public string ReviewerRole { get; set; } = ReviewParticipantRole.Buyer;
    public string RecipientRole { get; set; } = ReviewParticipantRole.Seller;
    public int Rating { get; set; }
    public string RatingType { get; set; } = ReviewRatingType.Positive;
    public string Comment { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime? RepliedAt { get; set; }
    public string RevisionStatus { get; set; } = ReviewRevisionStatus.None;
    public DateTime? RevisionRequestedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
    public User? Recipient { get; set; }
}

public static class ReviewParticipantRole
{
    public const string Buyer = "Buyer";
    public const string Seller = "Seller";
}

public static class ReviewRatingType
{
    public const string Positive = "Positive";
    public const string Neutral = "Neutral";
    public const string Negative = "Negative";

    public static string FromRating(int rating) => rating switch
    {
        >= 4 => Positive,
        3 => Neutral,
        _ => Negative
    };
}

public static class ReviewRevisionStatus
{
    public const string None = "None";
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
}
