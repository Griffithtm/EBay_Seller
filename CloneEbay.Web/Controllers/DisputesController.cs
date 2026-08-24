using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Authorize(Policy = "VerifiedSeller")]
[Route("seller/disputes")]
public class DisputesController(SellerApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page = 1)
    {
        var result = await api.GetDisputesAsync(status, fromDate, toDate, searchTerm, page);
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

    [HttpPost("resolve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id, string resolution)
    {
        var result = await api.ResolveDisputeAsync(id, resolution);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Đã giải quyết tranh chấp.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsv(string? status, DateTime? fromDate, DateTime? toDate, string? searchTerm)
    {
        var result = await api.GetDisputesAsync(status, fromDate, toDate, searchTerm, page: 1);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = "Không thể xuất dữ liệu.";
            return RedirectToAction(nameof(Index));
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("ID,Mã Đơn hàng,Người tạo,Mô tả,Trạng thái,Kết quả giải quyết");

        foreach (var item in result.Data.Items)
        {
            var desc = item.Description.Replace("\"", "\"\"");
            var res = (item.Resolution ?? string.Empty).Replace("\"", "\"\"");
            var statusVi = item.Status switch {
                "Open" => "Đang mở",
                "UnderReview" => "Đang xem xét",
                "Resolved" => "Đã giải quyết",
                _ => item.Status
            };
            builder.AppendLine($"{item.Id},#{item.OrderId},\"{item.RaiserUsername}\",\"{desc}\",\"{statusVi}\",\"{res}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        return File(bytes, "text/csv", $"danh-sach-tranh-chap-{DateTime.Now:yyyyMMdd}.csv");
    }
}

