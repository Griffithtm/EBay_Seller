using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface IDisputeRepository
{
    void Add(Dispute dispute);
    Task<(IReadOnlyList<Dispute> Items, int Total)> ListBySellerAsync(
        int sellerId, string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page, int pageSize, CancellationToken ct);

    Task<(IReadOnlyList<Dispute> Items, int Total)> ListByUserAsync(
        int userId, string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page, int pageSize, CancellationToken ct);

    Task<Dispute?> GetByIdAsync(int id, CancellationToken ct);
}
