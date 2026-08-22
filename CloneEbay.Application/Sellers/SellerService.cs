using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;
using CloneEbay.Domain.Enums;

namespace CloneEbay.Application.Sellers;

public sealed class SellerService(
    IUserRepository users,
    IStoreRepository stores,
    IUnitOfWork unitOfWork,
    IClock clock) : ISellerService
{
    public async Task<SellerVerificationResponse> ApplyAsync(
        int userId,
        SellerApplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw Problem(404, "USER_NOT_FOUND", "User was not found.");

        if (!user.IsActive)
        {
            throw Problem(403, "ACCOUNT_DISABLED", "This account has been disabled.");
        }

        var sellerType = ValidateApplication(request);
        var storeName = request.StoreName.Trim();
        var existing = await stores.GetBySellerIdAsync(userId, cancellationToken);

        if (existing?.VerificationStatus is VerificationStatus.Pending or VerificationStatus.Approved)
        {
            throw Problem(409, "APPLICATION_ALREADY_PENDING", "A pending or approved seller application already exists.");
        }

        if (await stores.StoreNameExistsAsync(storeName, existing?.Id, cancellationToken))
        {
            throw Problem(409, "STORE_NAME_ALREADY_EXISTS", "This store name is already in use.");
        }

        var now = clock.UtcNow;
        if (existing is null)
        {
            existing = new Store
            {
                SellerId = userId,
                CreatedAtUtc = now
            };
            await stores.AddAsync(existing, cancellationToken);
        }

        existing.StoreName = storeName;
        existing.SellerType = sellerType;
        existing.LegalName = request.LegalName.Trim();
        existing.ContactPhone = request.Phone.Trim();
        existing.VerificationStatus = VerificationStatus.Pending;
        existing.RejectionReason = null;
        existing.ReviewedAtUtc = null;
        existing.ReviewedByUserId = null;
        existing.UpdatedAtUtc = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapVerification(existing);
    }

    public async Task<SellerVerificationResponse> GetVerificationStatusAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var store = await stores.GetBySellerIdAsync(userId, cancellationToken);
        return store is null
            ? throw Problem(404, "STORE_NOT_FOUND", "No seller application was found for this user.")
            : MapVerification(store);
    }

    public async Task<PagedResult<SellerApplicationResponse>> GetApplicationsAsync(
        VerificationStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await stores.ListApplicationsAsync(status, page, pageSize, cancellationToken);
        return new PagedResult<SellerApplicationResponse>(
            result.Items.Select(MapApplication).ToList(),
            result.Page,
            result.PageSize,
            result.TotalItems);
    }

    public async Task<SellerApplicationResponse> ApproveAsync(
        int storeId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var store = await GetPendingApplicationAsync(storeId, cancellationToken);
        var now = clock.UtcNow;
        store.VerificationStatus = VerificationStatus.Approved;
        store.RejectionReason = null;
        store.ReviewedByUserId = adminUserId;
        store.ReviewedAtUtc = now;
        store.UpdatedAtUtc = now;
        store.Seller.Role = UserRole.Seller;
        store.Seller.UpdatedAtUtc = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapApplication(store);
    }

    public async Task<SellerApplicationResponse> RejectAsync(
        int storeId,
        int adminUserId,
        RejectSellerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500)
        {
            throw Problem(422, "REJECTION_REASON_INVALID", "A rejection reason of at most 500 characters is required.");
        }

        var store = await GetPendingApplicationAsync(storeId, cancellationToken);
        var now = clock.UtcNow;
        store.VerificationStatus = VerificationStatus.Rejected;
        store.RejectionReason = request.Reason.Trim();
        store.ReviewedByUserId = adminUserId;
        store.ReviewedAtUtc = now;
        store.UpdatedAtUtc = now;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return MapApplication(store);
    }

    public async Task<bool> IsVerifiedAsync(int userId, CancellationToken cancellationToken = default)
    {
        var store = await stores.GetBySellerIdAsync(userId, cancellationToken);
        return store?.VerificationStatus == VerificationStatus.Approved && store.Seller.IsActive;
    }

    private async Task<Store> GetPendingApplicationAsync(int storeId, CancellationToken cancellationToken)
    {
        var store = await stores.GetByIdAsync(storeId, cancellationToken)
            ?? throw Problem(404, "STORE_NOT_FOUND", "Seller application was not found.");

        if (store.VerificationStatus != VerificationStatus.Pending)
        {
            throw Problem(409, "ALREADY_PROCESSED", "Only pending applications can be reviewed.");
        }

        return store;
    }

    private static SellerType ValidateApplication(SellerApplyRequest request)
    {
        if (!Enum.TryParse<SellerType>(request.SellerType, true, out var sellerType) ||
            !Enum.IsDefined(sellerType))
        {
            throw Problem(422, "INVALID_SELLER_TYPE", "Seller type must be Individual or Business.");
        }

        if (request.StoreName.Trim().Length is < 3 or > 100)
        {
            throw Problem(422, "STORE_NAME_INVALID", "Store name must contain between 3 and 100 characters.");
        }

        if (request.LegalName.Trim().Length is < 2 or > 150)
        {
            throw Problem(422, "LEGAL_NAME_INVALID", "Legal name must contain between 2 and 150 characters.");
        }

        var phone = request.Phone.Trim();
        if (phone.Length is < 8 or > 20 || phone.Any(character => !char.IsDigit(character) && character is not '+' and not ' ' and not '-'))
        {
            throw Problem(422, "CONTACT_PHONE_INVALID", "A valid contact phone number is required.");
        }

        return sellerType;
    }

    private static SellerVerificationResponse MapVerification(Store store) =>
        new(true, store.Id, store.StoreName, store.VerificationStatus.ToString(), store.RejectionReason, store.CreatedAtUtc, store.ReviewedAtUtc);

    private static SellerApplicationResponse MapApplication(Store store) =>
        new(
            store.Id,
            store.SellerId,
            store.Seller.Email,
            store.Seller.Username,
            store.StoreName,
            store.SellerType.ToString(),
            store.LegalName,
            store.ContactPhone,
            store.VerificationStatus.ToString(),
            store.RejectionReason,
            store.CreatedAtUtc,
            store.ReviewedAtUtc);

    private static AppProblemException Problem(int statusCode, string code, string message) => new(statusCode, code, message);
}
