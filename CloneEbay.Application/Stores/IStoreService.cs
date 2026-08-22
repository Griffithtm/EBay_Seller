namespace CloneEbay.Application.Stores;

public interface IStoreService
{
    Task<StoreProfileResponse> GetPublicAsync(int sellerId, CancellationToken cancellationToken = default);
    Task<StoreProfileResponse> GetMineAsync(int sellerId, CancellationToken cancellationToken = default);
    Task<StoreProfileResponse> UpdateAsync(int sellerId, UpdateStoreProfileRequest request, CancellationToken cancellationToken = default);
}
