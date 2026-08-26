using CloneEbay.Application.Common;
using CloneEbay.Application.Orders;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task<PagedResult<Order>> GetSellerOrdersAsync(
        int sellerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Order?> GetSellerOrderByIdAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);

    Task AddShippingInfoAsync(ShippingInfo shippingInfo, CancellationToken cancellationToken = default);

    Task<ShippingInfo?> GetShippingInfoByOrderIdAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<Payment?> GetPaymentByOrderIdAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetActiveInFlightOrdersAsync(CancellationToken cancellationToken = default);
}
