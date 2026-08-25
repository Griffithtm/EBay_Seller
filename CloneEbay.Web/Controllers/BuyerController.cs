using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Authorize]
[Route("buyer")]
public class BuyerController(SellerApiClient api) : Controller
{
    // --- BUYER DISPUTES ---

    [HttpGet("disputes")]
    public async Task<IActionResult> Disputes(string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page = 1)
    {
        var result = await api.GetDisputesAsync(status, fromDate, toDate, searchTerm, page);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        ViewBag.StatusFilter = status;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.SearchTerm = searchTerm;
        return View("Disputes/Index", result.Data);
    }

    [HttpGet("disputes/create")]
    public IActionResult CreateDispute(int? orderId)
    {
        ViewBag.OrderId = orderId;
        return View("Disputes/Create");
    }

    [HttpPost("disputes/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDispute(int orderId, string description)
    {
        if (orderId <= 0 || string.IsNullOrWhiteSpace(description))
        {
            TempData["Error"] = "Vui lòng nhập Mã đơn hàng và lý do tranh chấp.";
            return View("Disputes/Create");
        }

        var result = await api.CreateDisputeAsync(orderId, description);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View("Disputes/Create");
        }

        TempData["Success"] = "Đã gửi khiếu nại/tranh chấp thành công.";
        return RedirectToAction(nameof(Disputes));
    }

    [HttpGet("disputes/{id:int}")]
    public async Task<IActionResult> DisputeDetail(int id)
    {
        var result = await api.GetDisputeByIdAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.ErrorMessage ?? "Không tìm thấy tranh chấp.";
            return RedirectToAction(nameof(Disputes));
        }

        return View("Disputes/Detail", result.Data);
    }

    [HttpPost("disputes/{id:int}/respond")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RespondDispute(int id, string message)
    {
        var result = await api.RespondToDisputeAsync(id, message);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Đã gửi tin nhắn phản hồi thành công.";
        }

        return RedirectToAction(nameof(DisputeDetail), new { id });
    }

    [HttpPost("disputes/{id:int}/close")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseDispute(int id)
    {
        var result = await api.CloseDisputeAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Đã đóng tranh chấp.";
        }

        return RedirectToAction(nameof(Disputes));
    }


    // --- BUYER RETURNS ---

    [HttpGet("returns")]
    public async Task<IActionResult> Returns(string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page = 1)
    {
        var result = await api.GetReturnsAsync(status, fromDate, toDate, searchTerm, page);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Home");
        }

        ViewBag.StatusFilter = status;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.SearchTerm = searchTerm;
        return View("Returns/Index", result.Data);
    }

    [HttpGet("returns/create")]
    public IActionResult CreateReturn(int? orderId)
    {
        ViewBag.OrderId = orderId;
        return View("Returns/Create");
    }

    [HttpPost("returns/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateReturn(int orderId, string reason, string preferredResolution)
    {
        if (orderId <= 0 || string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "Vui lòng nhập Mã đơn hàng và lý do đổi trả.";
            return View("Returns/Create");
        }

        var result = await api.CreateReturnAsync(orderId, reason, preferredResolution);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return View("Returns/Create");
        }

        TempData["Success"] = "Đã gửi yêu cầu đổi trả thành công.";
        return RedirectToAction(nameof(Returns));
    }

    [HttpPost("returns/{id:int}/shipping")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProvideShipping(int id, string returnCarrier, string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(returnCarrier) || string.IsNullOrWhiteSpace(trackingNumber))
        {
            TempData["Error"] = "Vui lòng điền đơn vị vận chuyển và mã vận đơn.";
            return RedirectToAction(nameof(Returns));
        }

        var result = await api.ProvideReturnShippingAsync(id, returnCarrier, trackingNumber);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Đã cập nhật thông tin gửi hàng trả về cho Người bán.";
        }

        return RedirectToAction(nameof(Returns));
    }
}
