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

    Task<OrderDetailResponse> GetSellerOrderDetailAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<OrderConfirmResponse> ConfirmOrderAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<UpdateShippingStatusResponse> UpdateShippingStatusAsync(
        int orderId,
        int sellerId,
        UpdateShippingStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<CreateShippingLabelResponse> CreateShippingLabelAsync(
        int orderId,
        int sellerId,
        CreateShippingLabelRequest request,
        CancellationToken cancellationToken = default);

    Task<ShippingInfoSummaryDto> GetShippingInfoAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<PaymentDetailResponse> GetPaymentInfoAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse> CreateMockOrderAsync(
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse> CreateManualOrderAsync(
        int sellerId,
        CreateManualOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse> PayVnPayAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<OrderConfirmResponse> CancelOrderAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);
}
