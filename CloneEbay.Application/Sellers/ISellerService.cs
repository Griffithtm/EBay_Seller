using CloneEbay.Application.Common;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Sellers;

public interface ISellerService
{
    Task<SellerVerificationResponse> ApplyAsync(int userId, SellerApplyRequest request, CancellationToken cancellationToken = default);
    Task<SellerVerificationResponse> GetVerificationStatusAsync(int userId, CancellationToken cancellationToken = default);
    Task<PagedResult<SellerApplicationResponse>> GetApplicationsAsync(VerificationStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<SellerApplicationResponse> ApproveAsync(int storeId, int adminUserId, CancellationToken cancellationToken = default);
    Task<SellerApplicationResponse> RejectAsync(int storeId, int adminUserId, RejectSellerRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsVerifiedAsync(int userId, CancellationToken cancellationToken = default);
}
