using CloneEbay.Application.Common;

namespace CloneEbay.Application.Disputes;

public sealed record DisputeResponse(
    int Id,
    int OrderId,
    int RaiserId,
    string RaiserUsername,
    string Description,
    string Status,
    string? Resolution);

public sealed record DisputeListQuery(
    int SellerId,
    string? Status,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 7);

public sealed record ResolveDisputeRequest(string Resolution);