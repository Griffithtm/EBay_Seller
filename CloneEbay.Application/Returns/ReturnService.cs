using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Returns;

public sealed class ReturnService(
    IReturnRepository returnRepository,
    IUnitOfWork unitOfWork) : IReturnService
{
    public async Task<ReturnRequestResponse> CreateReturnRequestAsync(int userId, CreateReturnRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new AppProblemException(400, "VALIDATION_ERROR", "Reason for return is required.");

        var rr = new ReturnRequest
        {
            OrderId = request.OrderId,
            UserId = userId,
            Reason = request.Reason.Trim(),
            PreferredResolution = request.PreferredResolution ?? "Refund",
            Status = ReturnRequestStatus.Requested,
            CreatedAt = DateTime.UtcNow
        };

        returnRepository.AddReturnRequest(rr);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await returnRepository.GetByIdAsync(rr.Id, ct);
        return Map(created ?? rr);
    }

    public async Task<PagedResult<ReturnRequestResponse>> GetReturnsAsync(ReturnRequestListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await returnRepository.ListBySellerAsync(
            query.UserId, query.Status, query.FromDate, query.ToDate, query.SearchTerm, query.Page, query.PageSize, ct);

        // Fallback to ListByUserAsync if items is empty
        if (total == 0)
        {
            (items, total) = await returnRepository.ListByUserAsync(
                query.UserId, query.Status, query.FromDate, query.ToDate, query.SearchTerm, query.Page, query.PageSize, ct);
        }

        var dtos = items.Select(Map).ToList();
        return new PagedResult<ReturnRequestResponse>(dtos, query.Page, query.PageSize, total);
    }

    public async Task<ReturnRequestResponse> GetReturnByIdAsync(int returnId, int userId, CancellationToken ct = default)
    {
        var rr = await returnRepository.GetByIdAsync(returnId, ct)
            ?? throw new AppProblemException(404, "RETURN_NOT_FOUND", "Return request not found.");

        return Map(rr);
    }

    public async Task<ReturnRequestResponse> UpdateReturnStatusAsync(int returnId, int sellerUserId, UpdateReturnStatusRequest request, CancellationToken ct = default)
    {
        var rr = await returnRepository.GetByIdAsync(returnId, ct)
            ?? throw new AppProblemException(404, "RETURN_NOT_FOUND", "Return request not found.");

        EnsureSellerOwnsOrder(rr, sellerUserId);

        if (!ReturnRequestStatus.IsValidTransition(rr.Status, request.Status))
            throw new AppProblemException(422, "INVALID_STATUS_TRANSITION",
                $"Cannot transition from '{rr.Status}' to '{request.Status}'.");

        if (request.Status == ReturnRequestStatus.Declined)
        {
            var lowerReason = rr.Reason.ToLowerInvariant();
            var isChangeOfMind = lowerReason.Contains("change") || lowerReason.Contains("đổi ý") || lowerReason.Contains("changemind");
            if (!isChangeOfMind)
                throw new AppProblemException(422, "DECLINE_NOT_ALLOWED",
                    "You can only decline return requests with 'change of mind' reason. Defective or misrepresented items must be accepted.");
        }

        rr.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.SellerNote))
            rr.SellerNote = request.SellerNote.Trim();
        if (request.RestockingFee.HasValue)
            rr.RestockingFee = request.RestockingFee.Value;
        if (request.RefundAmount.HasValue)
            rr.RefundAmount = request.RefundAmount.Value;

        rr.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return Map(rr);
    }

    public async Task<ReturnRequestResponse> ProvideReturnShippingAsync(int returnId, int buyerUserId, ProvideReturnShippingRequest request, CancellationToken ct = default)
    {
        var rr = await returnRepository.GetByIdAsync(returnId, ct)
            ?? throw new AppProblemException(404, "RETURN_NOT_FOUND", "Return request not found.");

        if (rr.UserId != buyerUserId)
            throw new AppProblemException(403, "FORBIDDEN_NOT_BUYER", "Only the buyer can provide return shipping tracking.");

        if (string.IsNullOrWhiteSpace(request.ReturnCarrier) || string.IsNullOrWhiteSpace(request.TrackingNumber))
            throw new AppProblemException(400, "VALIDATION_ERROR", "Return carrier and tracking number are required.");

        rr.ReturnCarrier = request.ReturnCarrier.Trim();
        rr.TrackingNumber = request.TrackingNumber.Trim();
        rr.BuyerShippedAt = DateTime.UtcNow;
        rr.Status = ReturnRequestStatus.InTransitBackToSeller;
        rr.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return Map(rr);
    }

    public async Task<ReturnRequestResponse> SendReplacementAsync(int returnId, int sellerUserId, SendReplacementRequest request, CancellationToken ct = default)
    {
        var rr = await returnRepository.GetByIdAsync(returnId, ct)
            ?? throw new AppProblemException(404, "RETURN_NOT_FOUND", "Return request not found.");

        EnsureSellerOwnsOrder(rr, sellerUserId);

        rr.ReturnCarrier = request.Carrier.Trim();
        rr.TrackingNumber = request.TrackingNumber.Trim();
        if (!string.IsNullOrWhiteSpace(request.SellerNote))
            rr.SellerNote = request.SellerNote.Trim();
        rr.Status = ReturnRequestStatus.ReplacementSent;
        rr.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(ct);
        return Map(rr);
    }

    public async Task<ReturnPolicyResponse?> GetStoreReturnPolicyAsync(int storeId, CancellationToken ct = default)
    {
        var policy = await returnRepository.GetReturnPolicyByStoreIdAsync(storeId, ct);
        return policy is null ? null : MapPolicy(policy);
    }

    public async Task<ReturnPolicyResponse> CreateOrUpdateReturnPolicyAsync(int sellerUserId, int storeId, CreateOrUpdateReturnPolicyRequest request, CancellationToken ct = default)
    {
        var policy = await returnRepository.GetReturnPolicyByStoreIdAsync(storeId, ct);
        if (policy is null)
        {
            policy = new ReturnPolicy
            {
                StoreId = storeId,
                AcceptReturns = request.AcceptReturns,
                ReturnPeriodDays = request.ReturnPeriodDays,
                ReturnShippingPaidBy = request.ReturnShippingPaidBy,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            returnRepository.AddReturnPolicy(policy);
        }
        else
        {
            policy.AcceptReturns = request.AcceptReturns;
            policy.ReturnPeriodDays = request.ReturnPeriodDays;
            policy.ReturnShippingPaidBy = request.ReturnShippingPaidBy;
            policy.Description = request.Description;
            policy.UpdatedAt = DateTime.UtcNow;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return MapPolicy(policy);
    }

    private static void EnsureSellerOwnsOrder(ReturnRequest rr, int sellerUserId)
    {
        if (rr.Order?.Items?.All(i => i.Product?.SellerId != sellerUserId) == true)
            throw new AppProblemException(403, "FORBIDDEN_NOT_OWNER", "This return request does not belong to your store.");
    }

    private static ReturnRequestResponse Map(ReturnRequest rr) => new(
        rr.Id,
        rr.OrderId,
        rr.UserId,
        rr.User?.Username ?? string.Empty,
        rr.Reason,
        rr.PreferredResolution ?? "Refund",
        rr.Status,
        rr.ReturnCarrier,
        rr.TrackingNumber,
        rr.BuyerShippedAt,
        rr.DeliveredAt,
        rr.RestockingFee,
        rr.RefundAmount,
        rr.SellerNote,
        rr.CreatedAt);

    private static ReturnPolicyResponse MapPolicy(ReturnPolicy p) => new(
        p.Id,
        p.StoreId,
        p.AcceptReturns,
        p.ReturnPeriodDays,
        p.ReturnShippingPaidBy,
        p.Description);
}
