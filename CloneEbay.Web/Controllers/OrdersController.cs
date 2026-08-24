using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Authorize(Roles = "Seller")]
[Route("orders")]
public sealed class OrdersController(SellerApiClient apiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? status = null, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetOrdersAsync(status, page, pageSize: 20, cancellationToken);
        ViewBag.CurrentStatus = status;
        return View(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetOrderDetailAsync(id, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("{id:int}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken = default)
    {
        var result = await apiClient.ConfirmOrderAsync(id, cancellationToken);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = "Đã xác nhận đơn hàng thành công và trừ tồn kho!";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:int}/shipping-label")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShippingLabel(int id, string carrier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(carrier))
        {
            TempData["ErrorMessage"] = "Vui lòng chọn đơn vị vận chuyển.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await apiClient.CreateShippingLabelAsync(id, carrier, cancellationToken);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = $"Đã tạo phiếu vận chuyển thành công! Mã vận đơn: {result.Data?.TrackingNumber}";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("{id:int}/status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateShippingStatus(int id, string status, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            TempData["ErrorMessage"] = "Vui lòng chọn trạng thái vận chuyển.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await apiClient.UpdateShippingStatusAsync(id, status, cancellationToken);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
        }
        else
        {
            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái vận chuyển sang {status}!";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }
}
