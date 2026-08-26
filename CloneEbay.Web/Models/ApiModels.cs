using System.Net;
using System.Text.Json.Serialization;

namespace CloneEbay.Web.Models;

public sealed record ApiErrorDto(string Code, string Message, object? Details);
public sealed record ApiMetaDto(string TraceId);
public sealed record ApiEnvelopeDto<T>(bool Success, T? Data, ApiErrorDto? Error, ApiMetaDto Meta);

public sealed record ApiCallResult<T>(bool Success, T? Data, ApiErrorDto? Error, HttpStatusCode StatusCode)
{
    public string ErrorMessage => Error?.Code switch
    {
        "EMAIL_ALREADY_EXISTS" => "Email này đã được đăng ký. Hãy đăng nhập hoặc dùng email khác.",
        "INVALID_CREDENTIALS" => "Email hoặc mật khẩu chưa chính xác.",
        "ACCOUNT_TEMPORARILY_LOCKED" => "Tài khoản tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau 15 phút.",
        "APPLICATION_ALREADY_PENDING" => "Bạn đã có hồ sơ đang chờ duyệt hoặc đã được phê duyệt.",
        "STORE_NAME_ALREADY_EXISTS" => "Tên cửa hàng này đã được sử dụng.",
        "SELLER_NOT_VERIFIED" => "Tài khoản người bán của bạn chưa được xác minh.",
        "STORE_NOT_FOUND" => "Không tìm thấy hồ sơ cửa hàng.",
        "INVALID_SELLER_TYPE" => "Loại người bán không hợp lệ.",
        "BANNER_SIZE_INVALID" => "Ảnh bìa phải nhỏ hơn 5 MB.",
        "BANNER_TYPE_INVALID" or "BANNER_CONTENT_INVALID" => "Ảnh bìa phải là tệp JPG, PNG hoặc WEBP hợp lệ.",
        "PRODUCT_NOT_FOUND" => "Không tìm thấy sản phẩm.",
        "CATEGORY_NOT_FOUND" => "Danh mục đã chọn không tồn tại.",
        "PRODUCT_IMAGES_INVALID" => "Vui lòng chọn từ 1 đến 8 ảnh sản phẩm.",
        "PRODUCT_IMAGE_SIZE_INVALID" => "Mỗi ảnh sản phẩm phải nhỏ hơn 5 MB.",
        "PRODUCT_IMAGE_TYPE_INVALID" or "PRODUCT_IMAGE_CONTENT_INVALID" => "Ảnh sản phẩm phải là tệp JPG, PNG hoặc WEBP hợp lệ.",
        "FORBIDDEN_NOT_OWNER" => "Bạn chỉ có thể quản lý sản phẩm thuộc cửa hàng của mình.",
        "PRODUCT_HAS_BIDS" => "Sản phẩm đã có lượt đấu giá nên không thể xóa. Hãy tạm ẩn sản phẩm.",
        "PRODUCT_HAS_ORDERS" => "Sản phẩm đã phát sinh đơn hàng nên không thể xóa. Hãy tạm ẩn sản phẩm.",
        "AUCTION_ALREADY_HAS_BID" or "AUCTION_PRICE_LOCKED" => "Không thể thay đổi hình thức hoặc giá khởi điểm sau khi đã có bid.",
        "AUCTION_END_TIME_REQUIRED" => "Thời điểm kết thúc đấu giá phải ở tương lai.",
        "QUANTITY_NEGATIVE" => "Tồn kho không được là số âm.",
        "PRODUCT_CREATE_RATE_LIMIT" => "Bạn đã đạt giới hạn đăng sản phẩm. Vui lòng thử lại sau.",
        "NOT_AN_AUCTION_PRODUCT" => "Sản phẩm này không phải listing đấu giá.",
        "SELLER_CANNOT_BID_OWN_PRODUCT" => "Bạn không thể đặt giá cho sản phẩm của chính mình.",
        "BID_TOO_LOW" => "Giá đặt phải cao hơn giá hiện tại.",
        "AUCTION_ALREADY_CLOSED" => "Phiên đấu giá đã kết thúc.",
        "AUCTION_NOT_ENDED" => "Chưa thể đóng phiên trước thời điểm kết thúc.",
        "UNAUTHORIZED" => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
        "FORBIDDEN" => "Bạn không có quyền thực hiện thao tác này.",
        "COUPON_NOT_FOUND" => "Không tìm thấy mã giảm giá.",
        "COUPON_CODE_INVALID" => "Mã giảm giá phải từ 3 đến 50 ký tự.",
        "COUPON_DISCOUNT_INVALID" => "Phần trăm giảm giá phải lớn hơn 0 và tối đa 100.",
        "COUPON_DATE_RANGE_INVALID" => "Ngày kết thúc phải sau ngày bắt đầu.",
        "COUPON_MAX_USAGE_INVALID" => "Số lượt sử dụng tối đa phải từ 1 trở lên.",
        "COUPON_MAX_USAGE_BELOW_USED" => "Số lượt tối đa không được thấp hơn số lượt đã dùng.",
        "COUPON_CODE_ALREADY_EXISTS" => "Mã này đã tồn tại cho sản phẩm đã chọn.",
        "COUPON_ALREADY_USED" => "Mã đã được sử dụng nên không thể xóa.",
        "RECAPTCHA_VERIFICATION_FAILED" => "Xác thực reCAPTCHA thất bại. Vui lòng thử lại.",
        // Sale events
        "NAME_ALREADY_EXISTS" => "Tên sự kiện này đã tồn tại.",
        "EMPTY_NAME" => "Vui lòng nhập tên.",
        "NAME_TOO_LONG" => "Tên quá dài.",
        "INVALID_DATE_RANGE" => "Ngày kết thúc phải sau ngày bắt đầu.",
        "INVALID_MODE_CONFIGURATION" => "Cấu hình hình thức sự kiện không hợp lệ.",
        "DISCOUNT_TIERS_REQUIRED" => "Cần ít nhất một mức giảm giá (tier).",
        "TOO_MANY_DISCOUNT_TIERS" => "Sự kiện chỉ được tối đa 10 tier.",
        "INVALID_DISCOUNT_VALUE" => "Giá trị giảm giá không hợp lệ.",
        "INVALID_DISCOUNT_PERCENTAGE" => "Phần trăm giảm giá phải từ 1 đến 90.",
        "DUPLICATE_TIER_PRIORITY" => "Thứ tự ưu tiên (priority) bị trùng giữa các tier.",
        "DUPLICATE_PRODUCT_ASSIGNMENT" => "Một sản phẩm chỉ được gán vào một tier.",
        "HIGHLIGHT_PERCENTAGE_REQUIRED" => "Vui lòng nhập phần trăm hiển thị nổi bật.",
        "INVALID_HIGHLIGHT_PERCENTAGE" => "Phần trăm hiển thị nổi bật phải từ 1 đến 90.",
        "INVALID_STATUS_TRANSITION" => "Không thể chuyển sang trạng thái này.",
        "EDIT_NOT_ALLOWED" => "Sự kiện đã kết thúc hoặc bị hủy nên không thể chỉnh sửa.",
        "LISTING_SELECTION_REQUIRED" => "Mỗi tier cần chọn ít nhất một sản phẩm.",
        "PRODUCTS_NOT_OWNED_BY_SELLER" => "Một số sản phẩm đã chọn không thuộc cửa hàng của bạn.",
        "SALE_EVENT_NOT_FOUND" => "Không tìm thấy sự kiện.",
        _ => Error?.Message ?? "Không thể hoàn tất yêu cầu. Vui lòng thử lại."
    };
}

public sealed record UserSessionDto(
    int Id,
    string Username,
    string Email,
    string Role,
    int? StoreId,
    [property: JsonPropertyName("verified")] bool SellerVerified);
public sealed record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc,
    UserSessionDto User);
public sealed record RegisterDto(int Id, string Username, string Email, string Role);

public sealed record SellerVerificationDto(
    bool HasApplication,
    int? StoreId,
    string? StoreName,
    [property: JsonPropertyName("verificationStatus")] string? Status,
    string? RejectionReason,
    DateTime? SubmittedAtUtc,
    DateTime? ReviewedAtUtc);

public sealed record SellerApplicationDto(
    int StoreId,
    int SellerId,
    string SellerEmail,
    string SellerUsername,
    string StoreName,
    string SellerType,
    string LegalName,
    string ContactPhone,
    [property: JsonPropertyName("verificationStatus")] string Status,
    string? RejectionReason,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc);

public sealed record PaginationDto(int Page, int PageSize, int TotalItems, int TotalPages);
public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, PaginationDto Pagination);

public sealed record StoreProfileDto(
    int Id,
    int SellerId,
    string SellerUsername,
    string StoreName,
    string? Description,
    [property: JsonPropertyName("bannerImageURL")] string? BannerImageUrl,
    string VerificationStatus,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CategoryDto(int Id, string Name);

public sealed record InventoryDto(int ProductId, int Quantity, DateTime LastUpdated);

public sealed record BidDto(int Id, int BidderId, string BidderUsername, decimal Amount, DateTime BidTime);

public sealed record BidListDto(IReadOnlyList<BidDto> Items, decimal? HighestBid, int TotalBids);

public sealed record ProductDto(
    int Id,
    string Title,
    string Description,
    decimal Price,
    IReadOnlyList<string> Images,
    int CategoryId,
    string CategoryName,
    int SellerId,
    string SellerUsername,
    string Status,
    bool IsAuction,
    DateTime? AuctionEndTime,
    DateTime? AuctionClosedAt,
    int? AuctionWinnerId,
    decimal? HighestBid,
    int BidCount,
    InventoryDto Inventory,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CouponDto(
    int Id,
    string Code,
    decimal DiscountPercent,
    DateTime StartDate,
    DateTime EndDate,
    int MaxUsage,
    int UsageCount,
    int ProductId,
    string ProductTitle,
    int SellerId,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record AuctionCloseDto(
    int ProductId,
    int? WinnerId,
    decimal? WinningAmount,
    int? OrderId,
    bool OrderCreationPending,
    DateTime ClosedAt);

// Sale events

public sealed record SaleEventTierDto(
    int Id,
    int Priority,
    string DiscountType,
    decimal DiscountValue,
    string? Label,
    IReadOnlyList<int> ProductIds);

public sealed record SaleEventDetailDto(
    int Id,
    string Name,
    string? Description,
    string Mode,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    bool OfferFreeShipping,
    bool IncludeSkippedItems,
    bool BlockPriceIncreaseRevisions,
    decimal? HighlightPercentage,
    IReadOnlyList<SaleEventTierDto> Tiers);

public sealed record SaleEventSummaryDto(
    int Id,
    string Name,
    string Status,
    DateTime StartDate,
    DateTime EndDate,
    int TierCount,
    int ProductCount,
    bool OfferFreeShipping,
    bool IncludeSkippedItems,
    bool BlockPriceIncreaseRevisions,
    decimal? HighlightPercentage);

public sealed record EligibleProductDto(
    int ProductId,
    string Title,
    string? ThumbnailUrl,
    int CategoryId,
    string CategoryName,
    decimal Price,
    DateTime CreatedAt,
    bool IsAlreadyAssigned);

public sealed record EligibleProductsPageDto(
    IReadOnlyList<EligibleProductDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize);
