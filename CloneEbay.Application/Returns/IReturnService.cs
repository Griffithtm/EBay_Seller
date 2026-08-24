using CloneEbay.Application.Common;

namespace CloneEbay.Application.Returns;

public interface IReturnService
{
    Task<PagedResult<ReturnRequestResponse>> GetReturnsAsync(ReturnRequestListQuery query, CancellationToken ct = default);
    Task<ReturnRequestResponse> GetReturnByIdAsync(int returnId, int sellerUserId, CancellationToken ct = default);
    Task<ReturnRequestResponse> UpdateReturnStatusAsync(int returnId, int sellerUserId, string newStatus, CancellationToken ct = default);
}
