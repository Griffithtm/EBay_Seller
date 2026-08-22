using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Stores;

public sealed class StoreService(
    IStoreRepository stores,
    IUnitOfWork unitOfWork,
    IClock clock) : IStoreService
{
    public async Task<StoreProfileResponse> GetPublicAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        var store = await stores.GetBySellerIdAsync(sellerId, cancellationToken)
            ?? throw Problem(404, "STORE_NOT_FOUND", "Store was not found.");

        if (store.VerificationStatus != VerificationStatus.Approved)
        {
            throw Problem(404, "STORE_NOT_FOUND", "Store was not found.");
        }

        return Map(store);
    }

    public async Task<StoreProfileResponse> GetMineAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        var store = await stores.GetBySellerIdAsync(sellerId, cancellationToken)
            ?? throw Problem(404, "STORE_NOT_FOUND", "Store was not found.");
        return Map(store);
    }

    public async Task<StoreProfileResponse> UpdateAsync(
        int sellerId,
        UpdateStoreProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var store = await stores.GetBySellerIdAsync(sellerId, cancellationToken)
            ?? throw Problem(404, "STORE_NOT_FOUND", "Store was not found.");

        if (store.VerificationStatus != VerificationStatus.Approved)
        {
            throw Problem(403, "SELLER_NOT_VERIFIED", "Only approved sellers can update a store profile.");
        }

        if (request.StoreName is not null)
        {
            var name = request.StoreName.Trim();
            if (name.Length is < 3 or > 100)
            {
                throw Problem(422, "STORE_NAME_INVALID", "Store name must contain between 3 and 100 characters.");
            }

            if (await stores.StoreNameExistsAsync(name, store.Id, cancellationToken))
            {
                throw Problem(409, "STORE_NAME_ALREADY_EXISTS", "This store name is already in use.");
            }

            store.StoreName = name;
        }

        if (request.Description?.Length > 2000)
        {
            throw Problem(422, "STORE_DESCRIPTION_TOO_LONG", "Store description cannot exceed 2000 characters.");
        }

        if (request.Description is not null)
        {
            store.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        if (request.BannerImageUrl is not null)
        {
            var bannerUrl = request.BannerImageUrl.Trim();
            if (bannerUrl.Length > 2048 ||
                (bannerUrl.Length > 0 &&
                 !bannerUrl.StartsWith('/') &&
                 (!Uri.TryCreate(bannerUrl, UriKind.Absolute, out var uri) ||
                  (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))))
            {
                throw Problem(422, "BANNER_URL_INVALID", "Banner URL must be a valid HTTP(S) URL or an application-relative path.");
            }

            store.BannerImageUrl = bannerUrl.Length == 0 ? null : bannerUrl;
        }
        store.UpdatedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(store);
    }

    private static StoreProfileResponse Map(Store store) =>
        new(
            store.Id,
            store.SellerId,
            store.Seller.Username,
            store.StoreName,
            store.Description,
            store.BannerImageUrl,
            store.VerificationStatus.ToString(),
            store.CreatedAtUtc,
            store.UpdatedAtUtc);

    private static AppProblemException Problem(int statusCode, string code, string message) => new(statusCode, code, message);
}
