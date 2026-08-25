using System.Text.Json.Serialization;

namespace CloneEbay.Web.Models;

public sealed record ReviewResponseDto(
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

public sealed record FeedbackResponseDto(
    int SellerId,
    decimal AverageRating,
    int TotalReviews,
    decimal PositiveRate);

public sealed record ReturnRequestResponseDto(
    int Id,
    int OrderId,
    int BuyerId,
    string BuyerUsername,
    string Reason,
    string PreferredResolution,
    string Status,
    string? ReturnCarrier,
    string? TrackingNumber,
    DateTime? BuyerShippedAt,
    DateTime? DeliveredAt,
    decimal? RestockingFee,
    decimal? RefundAmount,
    string? SellerNote,
    DateTime CreatedAt);

public sealed record DisputeMessageDto(
    int Id,
    int SenderId,
    string SenderUsername,
    string SenderRole,
    string Message,
    DateTime CreatedAt);

public sealed record DisputeEvidenceDto(
    int Id,
    int UploaderId,
    string UploaderUsername,
    string FilePath,
    string FileName,
    string ContentType,
    DateTime CreatedAt);

public sealed record DisputeResponseDto(
    int Id,
    int OrderId,
    int RaiserId,
    string RaiserUsername,
    string Description,
    string Status,
    string? Resolution,
    DateTime CreatedAt,
    IReadOnlyList<DisputeMessageDto> Messages,
    IReadOnlyList<DisputeEvidenceDto> Evidences);

public sealed record ReturnPolicyResponseDto(
    int Id,
    int StoreId,
    bool AcceptReturns,
    int ReturnPeriodDays,
    string ReturnShippingPaidBy,
    string? Description);

public sealed record BuyerItemSummaryDto(
    int Id,
    int OrderId,
    DateTime CreatedAt,
    string ProductTitle,
    int Rating,
    string Status,
    int TargetPage);

public sealed record BuyerSummaryResponseDto(
    int BuyerId,
    string Username,
    string Email,
    int TotalReviews,
    int TotalReturns,
    decimal AverageRatingGiven,
    IReadOnlyList<BuyerItemSummaryDto> Reviews,
    IReadOnlyList<BuyerItemSummaryDto> Returns);
