using CloneEbay.Application.Common;

namespace CloneEbay.Application.Returns;

public sealed record ReturnRequestResponse(
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

public sealed record ReturnRequestListQuery(
    int UserId,
    string? Status,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 7);

public sealed record CreateReturnRequest(
    int OrderId,
    string Reason,
    string? PreferredResolution = "Refund");

public sealed record UpdateReturnStatusRequest(
    string Status,
    string? SellerNote = null,
    decimal? RestockingFee = null,
    decimal? RefundAmount = null);

public sealed record ProvideReturnShippingRequest(
    string ReturnCarrier,
    string TrackingNumber);

public sealed record SendReplacementRequest(
    string Carrier,
    string TrackingNumber,
    string? SellerNote = null);

public sealed record ReturnPolicyResponse(
    int Id,
    int StoreId,
    bool AcceptReturns,
    int ReturnPeriodDays,
    string ReturnShippingPaidBy,
    string? Description);

public sealed record CreateOrUpdateReturnPolicyRequest(
    bool AcceptReturns,
    int ReturnPeriodDays,
    string ReturnShippingPaidBy,
    string? Description);