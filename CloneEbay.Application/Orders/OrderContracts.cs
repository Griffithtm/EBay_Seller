using System.ComponentModel.DataAnnotations;

namespace CloneEbay.Application.Orders;

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

public sealed record PaymentSummaryDto(
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
    IReadOnlyCollection<OrderItemDto> OrderItems,
    AddressDto? Address,
    PaymentSummaryDto? Payment,
    ShippingInfoSummaryDto? Shipping);

public sealed record OrderConfirmResponse(
    int Id,
    string Status);

public sealed record UpdateShippingStatusRequest(
    [Required] string Status);

public sealed record UpdateShippingStatusResponse(
    int Id,
    string Status);

public sealed record CreateShippingLabelRequest(
    [Required] string Carrier);

public sealed record CreateShippingLabelResponse(
    int OrderId,
    string Carrier,
    string TrackingNumber,
    string Status,
    string LabelUrl);
