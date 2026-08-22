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
