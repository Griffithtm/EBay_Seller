using System.Text.Json.Serialization;

namespace CloneEbay.Web.Models;

public sealed record ReviewResponseDto(
    int Id,
    int ProductId,
    string ProductTitle,
    int ReviewerId,
    string ReviewerUsername,
    int Rating,
    string Comment,
    string? Response,
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
    string Status,
    DateTime CreatedAt);

public sealed record DisputeResponseDto(
    int Id,
    int OrderId,
    int RaiserId,
    string RaiserUsername,
    string Description,
    string Status,
    string? Resolution);

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
