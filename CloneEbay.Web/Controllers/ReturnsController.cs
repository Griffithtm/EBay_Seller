using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Authorize(Policy = "VerifiedSeller")]
[Route("seller/returns")]
public class ReturnsController(SellerApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page = 1)
    {
        var result = await api.GetReturnsAsync(status, fromDate, toDate, searchTerm, page);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Seller");
        }

        ViewBag.StatusFilter = status;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.SearchTerm = searchTerm;
        return View(result.Data);
    }

    [HttpPost("update-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status)
    {
        var result = await api.UpdateReturnStatusAsync(id, status);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Đã cập nhật trạng thái yêu cầu đổi trả.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsv(string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm)
    {
        var result = await api.GetReturnsAsync(status, fromDate, toDate, searchTerm, page: 1);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = "Không thể xuất dữ liệu.";
            return RedirectToAction(nameof(Index));
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("ID,Ngày yêu cầu,Mã Đơn hàng,Khách hàng,Lý do,Trạng thái");

        foreach (var item in result.Data.Items)
        {
            var reason = item.Reason.Replace("\"", "\"\"");
            var statusVi = item.Status switch {
                "Requested" => "Đang yêu cầu",
                "Accepted" => "Đã chấp nhận",
                "Declined" => "Đã từ chối",
                "RefundOffered" => "Đề xuất hoàn tiền",
                "RefundedByReturn" => "Đã hoàn tiền",
                "Closed" => "Đã đóng",
                _ => item.Status
            };
            builder.AppendLine($"{item.Id},\"{item.CreatedAt:dd/MM/yyyy HH:mm}\",#{item.OrderId},\"{item.BuyerUsername}\",\"{reason}\",\"{statusVi}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        return File(bytes, "text/csv", $"danh-sach-doi-tra-{DateTime.Now:yyyyMMdd}.csv");
    }
}

