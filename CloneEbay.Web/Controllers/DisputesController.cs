using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Authorize(Policy = "VerifiedSeller")]
[Route("seller/disputes")]
public class DisputesController(SellerApiClient api, IWebHostEnvironment env) : Controller
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var result = await api.GetDisputeByIdAsync(id);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.ErrorMessage ?? "Không tìm thấy tranh chấp.";
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [HttpPost("respond")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(int id, string message)
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

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("evidence")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadEvidence(int id, IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn file đính kèm.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        var result = await api.UploadDisputeEvidenceAsync(id, file);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            try
            {
                var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
                var uploadsDir = Path.Combine(webRoot, "uploads", "disputes");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                if (result.Data?.Evidences != null && result.Data.Evidences.Count > 0)
                {
                    var lastEv = result.Data.Evidences.Last();
                    var localPath = Path.Combine(uploadsDir, lastEv.FileName);
                    using var stream = new FileStream(localPath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
            }
            catch { }
            TempData["Success"] = "Đã tải lên bằng chứng thành công!";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    [HttpPost("escalate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Escalate(int id)
    {
        var result = await api.EscalateDisputeAsync(id);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Đã leo thang tranh chấp lên Admin xử lý.";
        }

        return RedirectToAction(nameof(Index));
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
                "Escalated" => "Đã leo thang Admin",
                "Resolved" => "Đã giải quyết",
                "Closed" => "Đã đóng",
                _ => item.Status
            };
            builder.AppendLine($"{item.Id},#{item.OrderId},\"{item.RaiserUsername}\",\"{desc}\",\"{statusVi}\",\"{res}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        return File(bytes, "text/csv", $"danh-sach-tranh-chap-{DateTime.Now:yyyyMMdd}.csv");
    }
}
