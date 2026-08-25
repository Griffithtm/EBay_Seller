using CloneEbay.Application.Common;

namespace CloneEbay.Application.Reviews;

public sealed record ReviewResponse(
    int Id,
    int ProductId,
    string ProductTitle,
    int ReviewerId,
    string ReviewerUsername,
    int? RecipientId,
    string ReviewerRole,
    string RecipientRole,
    int Rating,
    string RatingType,
    string Comment,
    string? Response,
    DateTime? RepliedAt,
    string RevisionStatus,
    DateTime? RevisionRequestedAt,
    DateTime CreatedAt);

public sealed record FeedbackResponse(
    int SellerId,
    decimal AverageRating,
    int TotalReviews,
    decimal PositiveRate);

public sealed record ReviewListQuery(
    int? ProductId,
    int? SellerId,
    bool? HasReply,
    string? RatingType = null,
    string? RevisionStatus = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 7);

public sealed record ReplyToReviewRequest(string Response);

public sealed record UpdateReviewRevisionRequest(
    string? NewComment,
    int? NewRating);

public sealed record BuyerItemSummary(
    int Id,
    int OrderId,
    DateTime CreatedAt,
    string ProductTitle,
    int Rating,
    string Status,
    int TargetPage);

public sealed record BuyerSummaryResponse(
    int BuyerId,
    string Username,
    string Email,
    int TotalReviews,
    int TotalReturns,
    decimal AverageRatingGiven,
    IReadOnlyList<BuyerItemSummary> Reviews,
    IReadOnlyList<BuyerItemSummary> Returns);