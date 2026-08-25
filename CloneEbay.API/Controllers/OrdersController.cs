using System.ComponentModel.DataAnnotations;
using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Common;
using CloneEbay.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Authorize]
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
        var currentUserId = User.GetRequiredUserId();
        var result = await orderService.GetUserOrdersAsync(currentUserId, status, page, pageSize, cancellationToken);
        return Ok(ApiEnvelope<PagedResult<OrderSummaryDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> GetOrderDetail(
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.GetRequiredUserId();
        var result = await orderService.GetUserOrderDetailAsync(id, currentUserId, cancellationToken);
        return Ok(ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/confirm")]
    public async Task<ActionResult<ApiEnvelope<OrderConfirmResponse>>> ConfirmOrder(
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.ConfirmOrderAsync(id, currentSellerId, cancellationToken);
        return Ok(ApiEnvelope<OrderConfirmResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<ApiEnvelope<UpdateShippingStatusResponse>>> UpdateShippingStatus(
        int id,
        [FromBody] UpdateShippingStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.UpdateShippingStatusAsync(id, currentSellerId, request, cancellationToken);
        return Ok(ApiEnvelope<UpdateShippingStatusResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/shipping-label")]
    public async Task<ActionResult<ApiEnvelope<CreateShippingLabelResponse>>> CreateShippingLabel(
        int id,
        [FromBody] CreateShippingLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CreateShippingLabelAsync(id, currentSellerId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiEnvelope<CreateShippingLabelResponse>.Ok(result, HttpContext.TraceIdentifier));
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

    [HttpPost("mock")]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> CreateMockOrder(CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CreateMockOrderAsync(currentSellerId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("manual")]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> CreateManualOrder(
        [FromBody] CreateManualOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CreateManualOrderAsync(currentSellerId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/pay-vnpay")]
    public async Task<ActionResult<ApiEnvelope<OrderDetailResponse>>> PayVnPay(
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.PayVnPayAsync(id, currentSellerId, cancellationToken);
        return Ok(ApiEnvelope<OrderDetailResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<ApiEnvelope<OrderConfirmResponse>>> CancelOrder(
        int id,
        CancellationToken cancellationToken = default)
    {
        var currentSellerId = User.GetRequiredUserId();
        var result = await orderService.CancelOrderAsync(id, currentSellerId, cancellationToken);
        return Ok(ApiEnvelope<OrderConfirmResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
