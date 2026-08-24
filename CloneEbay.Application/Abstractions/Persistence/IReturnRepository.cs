using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface IReturnRepository
{
    Task<(IReadOnlyList<ReturnRequest> Items, int Total)> ListBySellerAsync(
        int sellerId, string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page, int pageSize, CancellationToken ct);

    Task<ReturnRequest?> GetByIdAsync(int id, CancellationToken ct);
}

