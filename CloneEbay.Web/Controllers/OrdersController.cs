using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Authorize(Roles = "Seller")]
[Route("orders")]
public sealed class OrdersController(SellerApiClient apiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? status = null,
        string? period = null,
        string? searchBy = null,
        string? search = null,
        string? sortBy = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetOrdersAsync(status, page, pageSize, cancellationToken);
        if (!result.Success && result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Auth");
        }
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentPeriod = period ?? "90";
        ViewBag.CurrentSearchBy = searchBy ?? "order_number";
        ViewBag.CurrentSearch = search;
        ViewBag.CurrentSortBy = sortBy ?? "date_paid_desc";
        ViewBag.CurrentPageSize = pageSize;
        return View(result);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        var productsResult = await apiClient.GetProductsAsync(page: 1, pageSize: 100, cancellationToken);
        ViewBag.Products = productsResult.Data?.Items ?? [];
        return View(new CreateManualOrderViewModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateManualOrderViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var productsResult = await apiClient.GetProductsAsync(page: 1, pageSize: 100, cancellationToken);
            ViewBag.Products = productsResult.Data?.Items ?? [];
            return View(model);
        }

        var result = await apiClient.CreateManualOrderAsync(model, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            var productsResult = await apiClient.GetProductsAsync(page: 1, pageSize: 100, cancellationToken);
            ViewBag.Products = productsResult.Data?.Items ?? [];
            return View(model);
        }

        var orderId = result.Data.Id;

        if (string.Equals(model.PaymentMethod, "VNPay Sandbox Gateway", StringComparison.OrdinalIgnoreCase))
        {
            var returnUrl = Url.Action("VNPayReturn", "Orders", new { id = orderId }, Request.Scheme);
            var vnpayResult = await apiClient.CreateVNPayCheckoutAsync(orderId, returnUrl, cancellationToken);
            if (vnpayResult.Success && !string.IsNullOrWhiteSpace(vnpayResult.Data?.PaymentUrl))
            {
                return Redirect(vnpayResult.Data.PaymentUrl);
            }
            return Redirect("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html");
        }

        if (string.Equals(model.PaymentMethod, "PayPal Sandbox Gateway", StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToAction(nameof(PayPalGateway), new { id = orderId });
        }

        TempData["SuccessMessage"] = $"Tạo đơn hàng #{orderId} thành công!";
        return RedirectToAction(nameof(Detail), new { id = orderId });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetOrderDetailAsync(id, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Auth");
            }
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpGet("{id:int}/paypal-gateway")]
    public async Task<IActionResult> PayPalGateway(int id, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetOrderDetailAsync(id, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
            return RedirectToAction(nameof(Index));
        }

        return View("PayPalGateway", result.Data);
    }

    [HttpGet("{id:int}/vnpay-return")]
    public async Task<IActionResult> VNPayReturn(int id, [FromQuery] string? vnp_ResponseCode, CancellationToken cancellationToken = default)
    {
        var queryDict = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        if (!queryDict.ContainsKey("vnp_ResponseCode"))
        {
            queryDict["vnp_ResponseCode"] = "00";
        }

        var result = await apiClient.CaptureVNPayCheckoutAsync(id, queryDict, cancellationToken);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = $"Thanh toán VNPay thành công cho Đơn hàng #{id}!";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:int}/paypal-checkout")]
    [ValidateAntiForgeryToken]
    public IActionResult PayPalCheckout(int id)
    {
        return RedirectToAction(nameof(PayPalGateway), new { id });
    }

    [HttpGet("{id:int}/paypal-return")]
    public async Task<IActionResult> PayPalReturn(int id, [FromQuery] string? token, CancellationToken cancellationToken = default)
    {
        var paypalToken = string.IsNullOrWhiteSpace(token) ? $"PAYPAL-ORDER-{id}-{Guid.NewGuid():N}" : token;
        var result = await apiClient.CapturePayPalCheckoutAsync(id, paypalToken, cancellationToken);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = $"Thanh toán PayPal thành công cho Đơn hàng #{id}!";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:int}/confirm-shipment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmShipment(int id, string trackingNumber, string carrierCode, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.PutAsync<OrderDetailDto>(
            $"api/orders/{id}/confirm-shipment",
            new { trackingNumber, carrierCode },
            authenticated: true,
            cancellationToken: cancellationToken);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = $"Đã bàn giao đơn hàng #{id} cho đơn vị vận chuyển!";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:int}/simulate-delivery")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SimulateDelivery(int id, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.SimulateDeliveryAsync(id, cancellationToken);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = $"Đơn hàng #{id} đã giao thành công!";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:int}/cancel")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.CancelOrderAsync(id, cancellationToken);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = $"Đã hủy Đơn hàng #{id} thành công!";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }
}
