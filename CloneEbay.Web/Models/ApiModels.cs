using System.ComponentModel.DataAnnotations;
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
        "SELLING_LIMIT_EXCEEDED" => Error?.Message ?? "Bạn đã đạt giới hạn đăng bán theo cấp độ tài khoản hiện tại.",
        "UNSUPPORTED_REPORT_SOURCE" => "Nguồn báo cáo không được hỗ trợ.",
        "UNRECOGNIZED_REPORT_TYPE" => "Loại báo cáo không hợp lệ với nguồn đã chọn.",
        "REPORT_RANGE_INVALID" => "Khoảng ngày kết thúc không được trước ngày bắt đầu.",
        "REPORT_TIMEZONE_INVALID" => "Múi giờ không hợp lệ.",
        "REPORT_FREQUENCY_INVALID" => "Tần suất lặp lại không hợp lệ.",
        "REPORT_DELIVERY_EMAIL_INVALID" => "Email nhận báo cáo không hợp lệ.",
        "REPORT_END_DATE_INVALID" => "Ngày kết thúc không được ở quá khứ.",
        "REPORT_SCHEDULE_DUPLICATE" => "Đã tồn tại lịch báo cáo đang hoạt động cho nguồn/loại này.",
        "REPORT_SCHEDULE_NOT_FOUND" => "Không tìm thấy lịch báo cáo.",
        "INVALID_PAYMENT_REPORT_PERIOD" => "Kỳ báo cáo không hợp lệ.",
        "INVALID_PAYMENT_REPORT_COMPARED" => "Tuỳ chọn so sánh không hợp lệ.",
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
        "ORDER_NOT_FOUND" => "Không tìm thấy đơn hàng hoặc đơn không thuộc cửa hàng của bạn.",
        "PAYMENT_NOT_COMPLETED" => "Đơn hàng chưa được thanh toán thành công nên chưa thể xác nhận.",
        "INSUFFICIENT_INVENTORY" => "Tồn kho không đủ để xác nhận đơn hàng này.",
        "INVALID_STATUS_TRANSITION" => "Chuyển trạng thái vận chuyển không hợp lệ theo quy trình.",
        "ORDER_NOT_CONFIRMED" => "Chỉ có thể in phiếu vận chuyển cho đơn hàng đã được xác nhận.",
        "SHIPPING_INFO_NOT_FOUND" => "Không tìm thấy thông tin vận chuyển cho đơn hàng.",
        "PAYMENT_NOT_FOUND" => "Không tìm thấy thông tin thanh toán cho đơn hàng.",
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

public sealed record PaymentReportSectionDto(
    decimal GrossSales, decimal Refunds, decimal Expenses, decimal Charges, decimal Payouts, decimal NetTransfersTotal);

public sealed record PaymentReportPeriodDto(DateTime FromUtc, DateTime ToUtc, PaymentReportSectionDto Figures, string? Note);

public sealed record PaymentReportResponseDto(string Currency, PaymentReportPeriodDto Current, PaymentReportPeriodDto? Comparison);

public sealed record PerformanceSummaryDto(
    string Period,
    decimal TotalRevenue,
    int TotalOrders,
    decimal SellingCosts,
    string SellingCostsNote,
    bool TrafficAvailable,
    string SellerLevel,
    string SellerLevelTitle,
    string SellerLevelMessage);

public sealed record PerformanceSalesReportDto(
    string Period, decimal TotalRevenue, int TotalOrders, decimal AverageOrderValue,
    int TotalBuyers, int OneTimeBuyers, int RepeatBuyers);

public sealed record PerformanceTrafficDto(bool Available, string Note);

public sealed record PerformanceServiceMetricsDto(
    decimal TransactionDefectRate, decimal LateShipmentRate, decimal TrackingUploadedOnTimeRate,
    decimal CasesClosedWithoutSellerResolutionRate, string Note);

public sealed record ReportDownloadDto(
    int Id,
    string ReferenceCode,
    string Source,
    string Type,
    string Status,
    DateTime RequestedAtUtc,
    DateTime? CompletedAtUtc,
    string? FileUrl,
    DateTime? RangeStartUtc,
    DateTime? RangeEndUtc);

public sealed record ReportScheduleDto(
    int Id,
    string Source,
    string Type,
    string Frequency,
    DateTime CreatedAtUtc,
    DateTime? LastRunAtUtc,
    DateTime? NextRunAtUtc,
    DateTime? EndDateUtc,
    bool IsActive,
    string? DeliveryEmail);

public sealed record SellerPerformanceLevelDto(
    int SellerId,
    string Level,
    decimal TransactionDefectRate,
    decimal LateShipmentRate,
    decimal TrackingUploadedOnTimeRate,
    decimal CasesClosedWithoutSellerResolutionRate,
    int TransactionsLast12Months,
    decimal SalesLast12Months,
    int MaxActiveListings,
    decimal MaxTotalActiveValue,
    int CurrentActiveListingCount,
    decimal CurrentActiveListingValue,
    DateTime? LastEvaluatedAtUtc,
    DateTime NextEvaluationDateUtc);

public sealed record SellerPerformanceWarningItemDto(
    string Metric,
    decimal CurrentValue,
    decimal Threshold,
    string Severity,
    string Message,
    string RecommendedAction);

public sealed record SellerPerformanceWarningDto(
    string? Severity,
    IReadOnlyList<SellerPerformanceWarningItemDto> Items,
    DateTime NextEvaluationDateUtc);

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

public sealed record OrderSummaryDto(
    int Id,
    DateTime OrderDate,
    decimal TotalPrice,
    string Status);

public sealed record OrderItemDto(
    int ProductId,
    string Title,
    int Quantity,
    decimal UnitPrice);

public sealed record AddressDto(
    int Id,
    string FullName,
    string Phone,
    string Street,
    string City,
    string? State,
    string Country);

public sealed record PaymentSummaryDto(string Method, string Status);

public sealed record PaymentDetailDto(
    decimal Amount,
    string Method,
    string Status,
    DateTime? PaidAt);

public sealed record ShippingInfoDto(
    string Carrier,
    string TrackingNumber,
    string Status,
    DateTime? EstimatedArrival);

public sealed record OrderDetailDto(
    int Id,
    string Status,
    IReadOnlyList<OrderItemDto> OrderItems,
    AddressDto? Address,
    PaymentSummaryDto? Payment,
    ShippingInfoDto? Shipping);

public sealed record OrderConfirmDto(int Id, string Status);
public sealed record UpdateShippingStatusResponseDto(int Id, string Status);
public sealed record ShippingLabelResponseDto(int OrderId, string Carrier, string TrackingNumber, string Status, string LabelUrl);

public sealed class CreateManualOrderViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ / số nhà.")]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập Thành phố / Tỉnh.")]
    public string City { get; set; } = string.Empty;

    public string? State { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Quốc gia.")]
    public string Country { get; set; } = "Vietnam";

    [Required(ErrorMessage = "Vui lòng chọn sản phẩm.")]
    public int ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 trở lên.")]
    public int Quantity { get; set; } = 1;

    [Range(0.01, 100000000, ErrorMessage = "Đơn giá không hợp lệ.")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn hình thức thanh toán.")]
    public string PaymentMethod { get; set; } = "COD";

    [Required(ErrorMessage = "Vui lòng chọn trạng thái thanh toán.")]
    public string PaymentStatus { get; set; } = "Completed";

    public IReadOnlyList<ProductDto> AvailableProducts { get; set; } = new List<ProductDto>();
}

