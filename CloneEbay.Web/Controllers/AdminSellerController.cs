using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Authorize(Roles = "Admin")]
[Route("admin/sellers")]
public sealed class AdminSellerController(SellerApiClient apiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string status = "Pending", CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetAsync<PagedResultDto<SellerApplicationDto>>(
            $"api/admin/seller?status={Uri.EscapeDataString(status)}&page=1&pageSize=100",
            cancellationToken: cancellationToken);
        if (!result.Success)
        {
            TempData["Error"] = result.ErrorMessage;
        }
        ViewData["Status"] = status;
        return View(result.Data?.Items ?? []);
    }

    [HttpPost("{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.PutAsync<SellerApplicationDto>($"api/admin/seller/{id}/approve", null, cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Đã phê duyệt hồ sơ Seller thành công." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{sellerId:int}/performance-level")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPerformanceLevel(int sellerId, string level, CancellationToken cancellationToken)
    {
        var result = await apiClient.SetSellerPerformanceLevelAsync(sellerId, level, cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? $"Đã đặt thủ công cấp độ tài khoản seller #{sellerId} thành {level}. Lần đánh giá tự động tiếp theo sẽ tính lại từ dữ liệu thật."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Index), new { status = Request.Query["status"].ToString() is { Length: > 0 } s ? s : "Approved" });
    }

    [HttpPost("{id:int}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason, CancellationToken cancellationToken)
    {
        var result = await apiClient.PutAsync<SellerApplicationDto>(
            $"api/admin/seller/{id}/reject",
            new { reason },
            cancellationToken: cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Đã gửi phản hồi yêu cầu Seller bổ sung hồ sơ." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }
}
