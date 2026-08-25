using CloneEbay.Application.Common;

namespace CloneEbay.Application.Returns;

public interface IReturnService
{
    Task<ReturnRequestResponse> CreateReturnRequestAsync(int userId, CreateReturnRequest request, CancellationToken ct = default);
    Task<PagedResult<ReturnRequestResponse>> GetReturnsAsync(ReturnRequestListQuery query, CancellationToken ct = default);
    Task<ReturnRequestResponse> GetReturnByIdAsync(int returnId, int userId, CancellationToken ct = default);
    Task<ReturnRequestResponse> UpdateReturnStatusAsync(int returnId, int sellerUserId, UpdateReturnStatusRequest request, CancellationToken ct = default);
    Task<ReturnRequestResponse> ProvideReturnShippingAsync(int returnId, int buyerUserId, ProvideReturnShippingRequest request, CancellationToken ct = default);
    Task<ReturnRequestResponse> SendReplacementAsync(int returnId, int sellerUserId, SendReplacementRequest request, CancellationToken ct = default);

    Task<ReturnPolicyResponse?> GetStoreReturnPolicyAsync(int storeId, CancellationToken ct = default);
    Task<ReturnPolicyResponse> CreateOrUpdateReturnPolicyAsync(int sellerUserId, int storeId, CreateOrUpdateReturnPolicyRequest request, CancellationToken ct = default);
}
