using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Returns;

public sealed class ReturnService(
    IReturnRepository returnRepository,
    IUnitOfWork unitOfWork) : IReturnService
{
    public async Task<PagedResult<ReturnRequestResponse>> GetReturnsAsync(ReturnRequestListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await returnRepository.ListBySellerAsync(
            query.SellerId, query.Status, query.FromDate, query.ToDate, query.SearchTerm, query.Page, query.PageSize, ct);

        var dtos = items.Select(Map).ToList();
        return new PagedResult<ReturnRequestResponse>(dtos, query.Page, query.PageSize, total);
    }

    public async Task<ReturnRequestResponse> GetReturnByIdAsync(int returnId, int sellerUserId, CancellationToken ct = default)
    {
        var rr = await returnRepository.GetByIdAsync(returnId, ct)
            ?? throw new AppProblemException(404, "RETURN_NOT_FOUND", "Return request not found.");

        EnsureSellerOwnsOrder(rr, sellerUserId);
        return Map(rr);
    }

    public async Task<ReturnRequestResponse> UpdateReturnStatusAsync(int returnId, int sellerUserId, string newStatus, CancellationToken ct = default)
    {
        var rr = await returnRepository.GetByIdAsync(returnId, ct)
            ?? throw new AppProblemException(404, "RETURN_NOT_FOUND", "Return request not found.");

        EnsureSellerOwnsOrder(rr, sellerUserId);

        // Validate state machine transition
        if (!ReturnRequestStatus.IsValidTransition(rr.Status, newStatus))
            throw new AppProblemException(422, "INVALID_STATUS_TRANSITION",
                $"Cannot transition from '{rr.Status}' to '{newStatus}'.");

        // Business rule: cannot Decline if reason is not change-of-mind
        if (newStatus == ReturnRequestStatus.Declined)
        {
            var lowerReason = rr.Reason.ToLowerInvariant();
            var isChangeOfMind = lowerReason.Contains("change") || lowerReason.Contains("đổi ý") || lowerReason.Contains("changemind");
            if (!isChangeOfMind)
                throw new AppProblemException(422, "DECLINE_NOT_ALLOWED",
                    "You can only decline return requests with 'change of mind' reason. Defective or misrepresented items must be accepted.");
        }

        rr.Status = newStatus;
        await unitOfWork.SaveChangesAsync(ct);
        return Map(rr);
    }

    private static void EnsureSellerOwnsOrder(ReturnRequest rr, int sellerUserId)
    {
        // The order's seller is checked via OrderItems — simplified: we verify the order contains seller's product
        // Full check is done at the repository query level (filtered by sellerId)
        // Here we just ensure the loaded entity belongs to that seller's context
        if (rr.Order?.Items?.All(i => i.Product?.SellerId != sellerUserId) == true)
            throw new AppProblemException(403, "FORBIDDEN_NOT_OWNER", "This return request does not belong to your store.");
    }

    private static ReturnRequestResponse Map(ReturnRequest rr) => new(
        rr.Id,
        rr.OrderId,
        rr.UserId,
        rr.User?.Username ?? string.Empty,
        rr.Reason,
        rr.Status,
        rr.CreatedAt);
}

