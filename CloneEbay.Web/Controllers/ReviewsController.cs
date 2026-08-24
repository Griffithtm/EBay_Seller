using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Authorize(Policy = "VerifiedSeller")]
[Route("seller/reviews")]
public class ReviewsController(SellerApiClient api) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(bool? hasReply, DateTime? fromDate, DateTime? toDate, string? searchTerm, int page = 1)
    {
        var result = await api.GetReviewsAsync(null, null, hasReply, fromDate, toDate, searchTerm, page);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction("Index", "Seller");
        }

        var store = await api.GetAsync<StoreProfileDto>("api/store/me");
        if (store.Success && store.Data is not null)
        {
            var feedback = await api.GetFeedbackAsync(store.Data.SellerId);
            if (feedback.Success && feedback.Data is not null)
            {
                ViewBag.Feedback = feedback.Data;
            }
        }

        ViewBag.HasReplyFilter = hasReply;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.SearchTerm = searchTerm;
        return View(result.Data);
    }

    [HttpPost("reply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string response)
    {
        var result = await api.ReplyToReviewAsync(id, response);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        else
        {
            TempData["Success"] = "Đã phản hồi đánh giá thành công.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsv(bool? hasReply, DateTime? fromDate, DateTime? toDate, string? searchTerm)
    {
        var result = await api.GetReviewsAsync(null, null, hasReply, fromDate, toDate, searchTerm, page: 1);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = "Không thể xuất dữ liệu.";
            return RedirectToAction(nameof(Index));
        }

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("ID,Ngày đánh giá,Sản phẩm,Khách hàng,Đánh giá (Số sao),Nội dung");

        foreach (var item in result.Data.Items)
        {
            var comment = item.Comment.Replace("\"", "\"\"");
            builder.AppendLine($"{item.Id},\"{item.CreatedAt:dd/MM/yyyy HH:mm}\",\"{item.ProductTitle}\",\"{item.ReviewerUsername}\",{item.Rating},\"{comment}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        return File(bytes, "text/csv", $"danh-sach-danh-gia-{DateTime.Now:yyyyMMdd}.csv");
    }
}

