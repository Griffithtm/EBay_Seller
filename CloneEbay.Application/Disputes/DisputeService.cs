using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Disputes;

public sealed class DisputeService(
    IDisputeRepository disputeRepository,
    IUnitOfWork unitOfWork) : IDisputeService
{
    public async Task<DisputeResponse> CreateDisputeAsync(int userId, CreateDisputeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new AppProblemException(400, "VALIDATION_ERROR", "Dispute description is required.");

        var dispute = new Dispute
        {
            OrderId = request.OrderId,
            RaisedBy = userId,
            Description = request.Description.Trim(),
            Status = DisputeStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        disputeRepository.Add(dispute);
        await unitOfWork.SaveChangesAsync(ct);

        var created = await disputeRepository.GetByIdAsync(dispute.Id, ct);
        return Map(created ?? dispute);
    }

    public async Task<PagedResult<DisputeResponse>> GetDisputesAsync(DisputeListQuery query, CancellationToken ct = default)
    {
        var (items, total) = await disputeRepository.ListBySellerAsync(
            query.UserId, query.Status, query.FromDate, query.ToDate, query.SearchTerm, query.Page, query.PageSize, ct);

        // Fallback to ListByUserAsync if items is empty
        if (total == 0)
        {
            (items, total) = await disputeRepository.ListByUserAsync(
                query.UserId, query.Status, query.FromDate, query.ToDate, query.SearchTerm, query.Page, query.PageSize, ct);
        }

        var dtos = items.Select(Map).ToList();
        return new PagedResult<DisputeResponse>(dtos, query.Page, query.PageSize, total);
    }

    public async Task<DisputeResponse> GetDisputeByIdAsync(int disputeId, int userId, CancellationToken ct = default)
    {
        var dispute = await disputeRepository.GetByIdAsync(disputeId, ct)
            ?? throw new AppProblemException(404, "DISPUTE_NOT_FOUND", "Dispute not found.");

        return Map(dispute);
    }

    public async Task<DisputeResponse> RespondToDisputeAsync(int disputeId, int userId, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new AppProblemException(400, "VALIDATION_ERROR", "Message cannot be empty.");

        var dispute = await disputeRepository.GetByIdAsync(disputeId, ct)
            ?? throw new AppProblemException(404, "DISPUTE_NOT_FOUND", "Dispute not found.");

        if (dispute.Status == DisputeStatus.Closed || dispute.Status == DisputeStatus.Resolved)
            throw new AppProblemException(400, "DISPUTE_CLOSED", "Cannot send messages to a closed or resolved dispute.");

        var isSeller = dispute.Order.Items.Any(i => i.Product.SellerId == userId);
        var isRaiser = dispute.RaisedBy == userId;
        var role = isSeller ? "Seller" : isRaiser ? "Buyer" : "Admin";

        dispute.Messages.Add(new DisputeMessage
        {
            DisputeId = disputeId,
            SenderId = userId,
            SenderRole = role,
            Message = message.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        dispute.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);

        var updated = await disputeRepository.GetByIdAsync(disputeId, ct);
        return Map(updated ?? dispute);
    }

    public async Task<DisputeResponse> UploadEvidenceAsync(int disputeId, int userId, string filePath, string fileName, string contentType, CancellationToken ct = default)
    {
        var dispute = await disputeRepository.GetByIdAsync(disputeId, ct)
            ?? throw new AppProblemException(404, "DISPUTE_NOT_FOUND", "Dispute not found.");

        dispute.Evidences.Add(new DisputeEvidence
        {
            DisputeId = disputeId,
            UploaderId = userId,
            FilePath = filePath,
            FileName = fileName,
            ContentType = contentType,
            CreatedAt = DateTime.UtcNow
        });

        dispute.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);

        var updated = await disputeRepository.GetByIdAsync(disputeId, ct);
        return Map(updated ?? dispute);
    }

    public async Task<DisputeResponse> EscalateDisputeAsync(int disputeId, int userId, CancellationToken ct = default)
    {
        var dispute = await disputeRepository.GetByIdAsync(disputeId, ct)
            ?? throw new AppProblemException(404, "DISPUTE_NOT_FOUND", "Dispute not found.");

        if (dispute.Status == DisputeStatus.Closed || dispute.Status == DisputeStatus.Resolved)
            throw new AppProblemException(400, "DISPUTE_CLOSED", "Dispute is already closed or resolved.");

        dispute.Status = DisputeStatus.Escalated;
        dispute.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);

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
        dispute.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);

        return Map(dispute);
    }

    public async Task<DisputeResponse> CloseDisputeAsync(int disputeId, int userId, CancellationToken ct = default)
    {
        var dispute = await disputeRepository.GetByIdAsync(disputeId, ct)
            ?? throw new AppProblemException(404, "DISPUTE_NOT_FOUND", "Dispute not found.");

        dispute.Status = DisputeStatus.Closed;
        dispute.UpdatedAt = DateTime.UtcNow;
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
        d.Resolution,
        d.CreatedAt,
        d.Messages.Select(m => new DisputeMessageDto(
            m.Id,
            m.SenderId,
            m.Sender?.Username ?? string.Empty,
            m.SenderRole,
            m.Message,
            m.CreatedAt)).ToList(),
        d.Evidences.Select(e => new DisputeEvidenceDto(
            e.Id,
            e.UploaderId,
            e.Uploader?.Username ?? string.Empty,
            e.FilePath,
            e.FileName,
            e.ContentType,
            e.CreatedAt)).ToList());
}
