using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Common;
using CloneEbay.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiEnvelope<PagedResult<OrderSummaryDto>>>> GetOrders(
        [FromQuery] int? sellerId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.GetSellerOrdersAsync(currentSellerId, status, page, pageSize, cancellationToken);
        return Ok(ApiEnvelope<PagedResult<OrderSummaryDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CreateManualOrderAsync(currentSellerId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> GetOrderDetail(
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.GetSellerOrderDetailAsync(id, currentSellerId, cancellationToken);
        return Ok(ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}/payment")]
    public async Task<ActionResult<ApiEnvelope<PaymentDetailResponse>>> GetPayment(
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.GetPaymentInfoAsync(id, currentSellerId, cancellationToken);
        return Ok(ApiEnvelope<PaymentDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ApiEnvelope<OrderStatusResponse>>> CancelOrder(
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CancelOrderAsync(id, currentSellerId, cancellationToken);
        return Ok(ApiEnvelope<OrderStatusResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/confirm-shipment")]
    public async Task<ActionResult<ApiEnvelope<ConfirmShipmentResponse>>> ConfirmShipment(
        int id,
        [FromBody] ConfirmShipmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.ConfirmShipmentAsync(id, currentSellerId, request, cancellationToken);
        return Ok(ApiEnvelope<ConfirmShipmentResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/paypal-checkout")]
    public async Task<ActionResult<ApiEnvelope<CreatePayPalOrderResponse>>> CreatePayPalCheckout(
        int id,
        [FromBody] CreatePayPalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CreatePayPalOrderAsync(id, currentSellerId, request, cancellationToken);
        return Ok(ApiEnvelope<CreatePayPalOrderResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/paypal-capture")]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> CapturePayPalCheckout(
        int id,
        [FromBody] CapturePayPalOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CapturePayPalOrderAsync(id, currentSellerId, request, cancellationToken);
        return Ok(ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/vnpay-checkout")]
    public async Task<ActionResult<ApiEnvelope<CreateVNPayOrderResponse>>> CreateVNPayCheckout(
        int id,
        [FromBody] CreateVNPayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CreateVNPayOrderAsync(id, currentSellerId, request, cancellationToken);
        return Ok(ApiEnvelope<CreateVNPayOrderResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/vnpay-capture")]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> CaptureVNPayCheckout(
        int id,
        [FromBody] CaptureVNPayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CaptureVNPayOrderAsync(id, currentSellerId, request, cancellationToken);
        return Ok(ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/simulate-delivery")]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> SimulateDelivery(
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.SimulateDeliveryAsync(id, currentSellerId, cancellationToken);
        return Ok(ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/tracking-status")]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> UpdateTrackingStatus(
        int id,
        [FromBody] UpdateTrackingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.UpdateTrackingStatusAsync(id, currentSellerId, request.TargetStatus, cancellationToken);
        return Ok(ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("payouts")]
    public async Task<ActionResult<ApiEnvelope<IReadOnlyCollection<SellerPayoutSummaryDto>>>> GetSellerPayouts(
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.GetSellerPayoutsAsync(currentSellerId, cancellationToken);
        return Ok(ApiEnvelope<IReadOnlyCollection<SellerPayoutSummaryDto>>.Ok(result, HttpContext.TraceIdentifier));
    }
}
