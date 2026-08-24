using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Disputes;

public sealed class DisputeService(
    IDisputeRepository disputeRepository,
    IUnitOfWork unitOfWork) : IDisputeService
{
    public async Task<PagedResult<DisputeResponse>> GetDisputesAsync(DisputeListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await disputeRepository.ListBySellerAsync(
            query.SellerId, query.Status, query.FromDate, query.ToDate, query.SearchTerm, query.Page, query.PageSize, ct);

        var dtos = items.Select(Map).ToList();
        return new PagedResult<DisputeResponse>(dtos, query.Page, query.PageSize, total);
    }

    public async Task<DisputeResponse> GetDisputeByIdAsync(int disputeId, int sellerUserId, CancellationToken ct = default)
    {
        var dispute = await disputeRepository.GetByIdAsync(disputeId, ct)
            ?? throw new AppProblemException(404, "DISPUTE_NOT_FOUND", "Dispute not found.");

        return Map(dispute);
    }

    public async Task<DisputeResponse> ResolveDisputeAsync(int disputeId, int resolverUserId, string resolution, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(resolution))
            throw new AppProblemException(400, "VALIDATION_ERROR", "Resolution is required.");

        var dispute = await disputeRepository.GetByIdAsync(disputeId, ct)
            ?? throw new AppProblemException(404, "DISPUTE_NOT_FOUND", "Dispute not found.");

        if (dispute.Status == DisputeStatus.Resolved)
            throw new AppProblemException(409, "DISPUTE_ALREADY_RESOLVED",
                "This dispute has already been resolved and cannot be changed.");

        dispute.Status = DisputeStatus.Resolved;
        dispute.Resolution = resolution;
        await unitOfWork.SaveChangesAsync(ct);

        return Map(dispute);
    }

    private static DisputeResponse Map(Dispute d) => new(
        d.Id,
        d.OrderId,
        d.RaisedBy,
        d.Raiser?.Username ?? string.Empty,
        d.Description,
        d.Status,
        d.Resolution);
}

