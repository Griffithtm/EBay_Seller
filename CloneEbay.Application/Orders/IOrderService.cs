using CloneEbay.Application.Common;

namespace CloneEbay.Application.Orders;

public interface IOrderService
{
    Task<PagedResult<OrderSummaryDto>> GetSellerOrdersAsync(
        int sellerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse> CreateManualOrderAsync(
        int sellerId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse> GetSellerOrderDetailAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<PaymentDetailResponse> GetPaymentInfoAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<ShippingInfoSummaryDto> GetShippingInfoAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<OrderStatusResponse> CancelOrderAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<ConfirmShipmentResponse> ConfirmShipmentAsync(
        int orderId,
        int sellerId,
        ConfirmShipmentRequest request,
        CancellationToken cancellationToken = default);

    Task<CreatePayPalOrderResponse> CreatePayPalOrderAsync(
        int orderId,
        int sellerId,
        CreatePayPalOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse> CapturePayPalOrderAsync(
        int orderId,
        int sellerId,
        CapturePayPalOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<CreateVNPayOrderResponse> CreateVNPayOrderAsync(
        int orderId,
        int sellerId,
        CreateVNPayOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse> CaptureVNPayOrderAsync(
        int orderId,
        int sellerId,
        CaptureVNPayOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse> SimulateDeliveryAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SellerPayoutSummaryDto>> GetSellerPayoutsAsync(
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<int> AutoProgressOrdersAsync(CancellationToken cancellationToken = default);
}
