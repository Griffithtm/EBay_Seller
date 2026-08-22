using System.Text.Json.Serialization;

namespace CloneEbay.Application.Stores;

public sealed record UpdateStoreProfileRequest(
    string? StoreName,
    string? Description,
    [property: JsonPropertyName("bannerImageURL")] string? BannerImageUrl);

public sealed record StoreProfileResponse(
    int Id,
    int SellerId,
    string SellerUsername,
    string StoreName,
    string? Description,
    [property: JsonPropertyName("bannerImageURL")] string? BannerImageUrl,
    string VerificationStatus,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
