using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface IStoreRepository
{
    Task<Store?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Store?> GetBySellerIdAsync(int sellerId, CancellationToken cancellationToken = default);
    Task<bool> StoreNameExistsAsync(string storeName, int? excludingStoreId, CancellationToken cancellationToken = default);
    Task<PagedResult<Store>> ListApplicationsAsync(
        VerificationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Store store, CancellationToken cancellationToken = default);
}
