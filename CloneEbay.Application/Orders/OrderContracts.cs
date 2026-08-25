using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Application.Orders;

public sealed record OrderSummaryDto(
    int Id,
    DateTime OrderDate,
    decimal TotalPrice,
    decimal FinalValueFee,
    decimal NetPayoutAmount,
    string Status,
    string PayoutStatus = "Pending",
    string? ProductName = null,
    int? TotalQuantity = null);

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

public sealed record PaymentSummaryDto(
    string Method,
    string Status);

public sealed record PaymentDetailResponse(
    decimal Amount,
    string Method,
    string Status,
    DateTime? PaidAt);

public sealed record ShippingInfoSummaryDto(
    string Carrier,
    string TrackingNumber,
    string Status,
    DateTime? EstimatedArrival);

public sealed record OrderDetailResponse(
    int Id,
    string Status,
    decimal TotalPrice,
    decimal FinalValueFee,
    decimal NetPayoutAmount,
    string PayoutStatus,
    IReadOnlyCollection<OrderItemDto> OrderItems,
    AddressDto? Address,
    PaymentSummaryDto? Payment,
    ShippingInfoSummaryDto? Shipping);

public sealed record SellerPayoutSummaryDto(
    int OrderId,
    DateTime OrderDate,
    decimal TotalPrice,
    decimal FinalValueFee,
    decimal NetPayoutAmount,
    string PayoutStatus);

public sealed record OrderStatusResponse(
    int Id,
    string Status);

public sealed record ConfirmShipmentRequest(
    [Required] string TrackingNumber,
    [Required] string CarrierCode);

public sealed record ConfirmShipmentResponse(
    int OrderId,
    string Status,
    string TrackingNumber,
    string CarrierCode);

public sealed record CreatePayPalOrderRequest(
    string? ReturnUrl,
    string? CancelUrl);

public sealed record CreatePayPalOrderResponse(
    int OrderId,
    string PaypalOrderId,
    string ApprovalUrl);

public sealed record CapturePayPalOrderRequest(
    [Required] string PaypalOrderId);

public sealed record CreateVNPayOrderRequest(
    string? ReturnUrl);

public sealed record CreateVNPayOrderResponse(
    int OrderId,
    string PaymentUrl);

public sealed record CaptureVNPayOrderRequest(
    Dictionary<string, string> QueryParams);

public sealed record CreateOrderItemRequest(
    [Required] int ProductId,
    [Range(1, 100000)] int Quantity,
    [Range(0.01, 1000000000)] decimal? UnitPrice);

public sealed record CreateOrderRequest(
    int? BuyerId,
    string? BuyerUsername,
    string? BuyerFullName,
    string? BuyerPhone,
    string? Country,
    string? City,
    string? District,
    string? Street,
    string? ZipCode,
    string? PaymentMethod,
    [Required] IReadOnlyList<CreateOrderItemRequest> Items);
