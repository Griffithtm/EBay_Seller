using CloneEbay.Application.Common;

namespace CloneEbay.Application.Disputes;

public interface IDisputeService
{
    Task<PagedResult<DisputeResponse>> GetDisputesAsync(DisputeListQuery query, CancellationToken ct = default);
    Task<DisputeResponse> GetDisputeByIdAsync(int disputeId, int sellerUserId, CancellationToken ct = default);
    Task<DisputeResponse> ResolveDisputeAsync(int disputeId, int resolverUserId, string resolution, CancellationToken ct = default);
}
