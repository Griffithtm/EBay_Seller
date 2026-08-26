using CloneEbay.Application.Common;

namespace CloneEbay.Application.Returns;

public sealed record ReturnRequestResponse(
    int Id,
    int OrderId,
    int BuyerId,
    string BuyerUsername,
    string Reason,
    string Status,
    DateTime CreatedAt);

public sealed record ReturnRequestListQuery(
    int SellerId,
    string? Status,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 7);

public sealed record UpdateReturnStatusRequest(string Status);