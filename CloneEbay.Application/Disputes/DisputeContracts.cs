using CloneEbay.Application.Common;

namespace CloneEbay.Application.Disputes;

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

public sealed record DisputeResponse(
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

public sealed record DisputeListQuery(
    int UserId,
    string? Status,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 7);

public sealed record CreateDisputeRequest(
    int OrderId,
    string Description);

public sealed record ResolveDisputeRequest(string Resolution);

public sealed record RespondToDisputeRequest(string Message);