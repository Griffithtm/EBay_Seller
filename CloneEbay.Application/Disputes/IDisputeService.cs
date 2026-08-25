using CloneEbay.Application.Common;

namespace CloneEbay.Application.Disputes;

public interface IDisputeService
{
    Task<DisputeResponse> CreateDisputeAsync(int userId, CreateDisputeRequest request, CancellationToken ct = default);
    Task<PagedResult<DisputeResponse>> GetDisputesAsync(DisputeListQuery query, CancellationToken ct = default);
    Task<DisputeResponse> GetDisputeByIdAsync(int disputeId, int userId, CancellationToken ct = default);
    Task<DisputeResponse> RespondToDisputeAsync(int disputeId, int userId, string message, CancellationToken ct = default);
    Task<DisputeResponse> UploadEvidenceAsync(int disputeId, int userId, string filePath, string fileName, string contentType, CancellationToken ct = default);
    Task<DisputeResponse> EscalateDisputeAsync(int disputeId, int userId, CancellationToken ct = default);
    Task<DisputeResponse> ResolveDisputeAsync(int disputeId, int resolverUserId, string resolution, CancellationToken ct = default);
    Task<DisputeResponse> CloseDisputeAsync(int disputeId, int userId, CancellationToken ct = default);
}
