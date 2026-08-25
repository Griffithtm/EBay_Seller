using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface IReturnRepository
{
    void AddReturnRequest(ReturnRequest returnRequest);
    Task<(IReadOnlyList<ReturnRequest> Items, int Total)> ListBySellerAsync(
        int sellerId, string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page, int pageSize, CancellationToken ct);

    Task<(IReadOnlyList<ReturnRequest> Items, int Total)> ListByUserAsync(
        int userId, string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page, int pageSize, CancellationToken ct);

    Task<ReturnRequest?> GetByIdAsync(int id, CancellationToken ct);

    Task<ReturnPolicy?> GetReturnPolicyByStoreIdAsync(int storeId, CancellationToken ct);
    void AddReturnPolicy(ReturnPolicy policy);
}
