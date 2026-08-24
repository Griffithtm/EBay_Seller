using CloneEbay.Application.Abstractions.Persistence;
using CloneEbay.Application.Abstractions.Time;
using CloneEbay.Application.Common;
using CloneEbay.Domain.Entities;

namespace CloneEbay.Application.Orders;

public sealed class OrderService(
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IOrderService
{
    public async Task<PagedResult<OrderSummaryDto>> GetSellerOrdersAsync(
        int sellerId,
        string? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedOrders = await orderRepository.GetSellerOrdersAsync(sellerId, status, page, pageSize, cancellationToken);

        var items = pagedOrders.Items.Select(order =>
        {
            var sellerSubtotal = order.Items
                .Where(item => item.Product.SellerId == sellerId)
                .Sum(item => item.UnitPrice * item.Quantity);

            return new OrderSummaryDto(
                order.Id,
                order.OrderDateUtc,
                sellerSubtotal,
                order.Status);
        }).ToList();

        return new PagedResult<OrderSummaryDto>(
            items,
            pagedOrders.Page,
            pagedOrders.PageSize,
            pagedOrders.TotalItems);
    }

    public async Task<OrderDetailResponse> GetSellerOrderDetailAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        var sellerItems = order.Items
            .Where(item => item.Product.SellerId == sellerId)
            .Select(item => new OrderItemDto(
                item.ProductId,
                item.Product.Title,
                item.Quantity,
                item.UnitPrice))
            .ToList();

        AddressDto? addressDto = order.Address is null ? null : new AddressDto(
            order.Address.Id,
            order.Address.FullName,
            order.Address.Phone,
            order.Address.Street,
            order.Address.City,
            order.Address.State,
            order.Address.Country);

        PaymentSummaryDto? paymentDto = order.Payment is null ? null : new PaymentSummaryDto(order.Payment.Status);

        ShippingInfoSummaryDto? shippingDto = order.ShippingInfo is null ? null : new ShippingInfoSummaryDto(
            order.ShippingInfo.Carrier,
            order.ShippingInfo.TrackingNumber,
            order.ShippingInfo.Status,
            order.ShippingInfo.EstimatedArrivalUtc);

        return new OrderDetailResponse(
            order.Id,
            order.Status,
            sellerItems,
            addressDto,
            paymentDto,
            shippingDto);
    }

    public async Task<OrderConfirmResponse> ConfirmOrderAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        if (order.Payment is null)
        {
            order.Payment = new Payment
            {
                OrderId = order.Id,
                UserId = order.BuyerId,
                Amount = order.TotalPrice,
                Method = "VNPay",
                Status = "Completed",
                PaidAtUtc = clock.UtcNow
            };
        }
        else
        {
            order.Payment.Status = "Completed";
            order.Payment.PaidAtUtc ??= clock.UtcNow;
        }

        if (string.Equals(order.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppProblemException(409, "INVALID_ORDER_STATUS", "Cancelled orders cannot be confirmed.");
        }

        var sellerItems = order.Items.Where(item => item.Product.SellerId == sellerId).ToList();

        foreach (var item in sellerItems)
        {
            if (item.Product.Inventory is null || item.Product.Inventory.Quantity < item.Quantity)
            {
                throw new AppProblemException(409, "INSUFFICIENT_INVENTORY", $"Insufficient inventory for product '{item.Product.Title}'.");
            }
        }

        var now = clock.UtcNow;
        foreach (var item in sellerItems)
        {
            item.Product.Inventory!.Quantity -= item.Quantity;
            item.Product.Inventory.LastUpdatedUtc = now;
        }

        order.Status = "Confirmed";
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderConfirmResponse(order.Id, order.Status);
    }

    public async Task<UpdateShippingStatusResponse> UpdateShippingStatusAsync(
        int orderId,
        int sellerId,
        UpdateShippingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        var currentShippingStatus = order.ShippingInfo?.Status ?? "Preparing";
        var targetShippingStatus = request.Status.Trim();

        if (!IsValidShippingStatusTransition(currentShippingStatus, targetShippingStatus))
        {
            throw new AppProblemException(422, "INVALID_STATUS_TRANSITION", $"Cannot transition shipping status from '{currentShippingStatus}' to '{targetShippingStatus}'.");
        }

        if (order.ShippingInfo is null)
        {
            order.ShippingInfo = new ShippingInfo
            {
                OrderId = order.Id,
                Carrier = "Standard",
                TrackingNumber = $"TRACK-{order.Id}",
                Status = targetShippingStatus,
                EstimatedArrivalUtc = clock.UtcNow.AddDays(3)
            };
            await orderRepository.AddShippingInfoAsync(order.ShippingInfo, cancellationToken);
        }
        else
        {
            order.ShippingInfo.Status = targetShippingStatus;
        }

        if (string.Equals(targetShippingStatus, "HandedToCarrier", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(targetShippingStatus, "InTransit", StringComparison.OrdinalIgnoreCase))
        {
            order.Status = "Shipped";
        }
        else if (string.Equals(targetShippingStatus, "Delivered", StringComparison.OrdinalIgnoreCase))
        {
            order.Status = "Delivered";
        }
        else if (string.Equals(targetShippingStatus, "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            order.Status = "Cancelled";
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateShippingStatusResponse(order.Id, order.Status);
    }

    public async Task<CreateShippingLabelResponse> CreateShippingLabelAsync(
        int orderId,
        int sellerId,
        CreateShippingLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
        if (order is null)
        {
            throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
        }

        if (!string.Equals(order.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppProblemException(409, "ORDER_NOT_CONFIRMED", "Shipping label can only be created for confirmed orders.");
        }

        var carrier = request.Carrier.Trim();
        var trackingNumber = $"{carrier.ToUpperInvariant()}-{clock.UtcNow:yyyyMMddHHmmss}{order.Id:D4}";
        var labelUrl = $"https://localhost:7000/api/shipping/labels/{order.Id}.pdf";

        if (order.ShippingInfo is null)
        {
            order.ShippingInfo = new ShippingInfo
            {
                OrderId = order.Id,
                Carrier = carrier,
                TrackingNumber = trackingNumber,
                Status = "LabelCreated",
                EstimatedArrivalUtc = clock.UtcNow.AddDays(3)
            };
            await orderRepository.AddShippingInfoAsync(order.ShippingInfo, cancellationToken);
        }
        else
        {
            order.ShippingInfo.Carrier = carrier;
            order.ShippingInfo.TrackingNumber = trackingNumber;
            order.ShippingInfo.Status = "LabelCreated";
            order.ShippingInfo.EstimatedArrivalUtc ??= clock.UtcNow.AddDays(3);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateShippingLabelResponse(
            order.Id,
            carrier,
            trackingNumber,
            "LabelCreated",
            labelUrl);
    }

    public async Task<ShippingInfoSummaryDto> GetShippingInfoAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var shippingInfo = await orderRepository.GetShippingInfoByOrderIdAsync(orderId, sellerId, cancellationToken);
        if (shippingInfo is null)
        {
            var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
            if (order is null)
            {
                throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
            }

            throw new AppProblemException(404, "SHIPPING_INFO_NOT_FOUND", "Shipping information not found for this order.");
        }

        return new ShippingInfoSummaryDto(
            shippingInfo.Carrier,
            shippingInfo.TrackingNumber,
            shippingInfo.Status,
            shippingInfo.EstimatedArrivalUtc);
    }

    public async Task<PaymentDetailResponse> GetPaymentInfoAsync(
        int orderId,
        int sellerId,
        CancellationToken cancellationToken = default)
    {
        var payment = await orderRepository.GetPaymentByOrderIdAsync(orderId, sellerId, cancellationToken);
        if (payment is null)
        {
            var order = await orderRepository.GetSellerOrderByIdAsync(orderId, sellerId, cancellationToken);
            if (order is null)
            {
                throw new AppProblemException(404, "ORDER_NOT_FOUND", "Order was not found or does not contain items for this seller.");
            }

            throw new AppProblemException(404, "PAYMENT_NOT_FOUND", "Payment information not found for this order.");
        }

        return new PaymentDetailResponse(
            payment.Amount,
            payment.Method,
            payment.Status,
            payment.PaidAtUtc);
    }

    private static bool IsValidShippingStatusTransition(string currentStatus, string targetStatus)
    {
        if (string.Equals(currentStatus, targetStatus, StringComparison.OrdinalIgnoreCase))
            return true;

        return (currentStatus.ToUpperInvariant(), targetStatus.ToUpperInvariant()) switch
        {
            ("PREPARING", "LABELCREATED") => true,
            ("PREPARING", "HANDEDTOCARRIER") => true,
            ("PREPARING", "CANCELLED") => true,

            ("LABELCREATED", "HANDEDTOCARRIER") => true,
            ("LABELCREATED", "CANCELLED") => true,

            ("HANDEDTOCARRIER", "INTRANSIT") => true,
            ("HANDEDTOCARRIER", "DELIVERED") => true,
            ("HANDEDTOCARRIER", "CANCELLED") => true,

            ("INTRANSIT", "DELIVERED") => true,
            ("INTRANSIT", "CANCELLED") => true,

            _ => false
        };
    }
}
