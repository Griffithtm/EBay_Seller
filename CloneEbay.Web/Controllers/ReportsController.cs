using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Route("reports")]
[Authorize(Policy = "VerifiedSeller")]
public sealed class ReportsController(SellerApiClient apiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var downloadsTask = apiClient.GetReportDownloadsAsync(cancellationToken);
        var schedulesTask = apiClient.GetReportSchedulesAsync(cancellationToken);
        await Task.WhenAll(downloadsTask, schedulesTask);
        var downloadsResult = await downloadsTask;
        var schedulesResult = await schedulesTask;

        if (!downloadsResult.Success) TempData["Error"] = downloadsResult.ErrorMessage;
        else if (!schedulesResult.Success) TempData["Error"] = schedulesResult.ErrorMessage;

        return View(new ReportsIndexViewModel
        {
            Downloads = downloadsResult.Data?.Items ?? [],
            Schedules = schedulesResult.Data?.Items ?? []
        });
    }

    [HttpPost("downloads")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDownload(CreateReportDownloadViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng điền đầy đủ thông tin xuất báo cáo.";
            return RedirectToAction(nameof(Index));
        }

        var result = await apiClient.CreateReportDownloadAsync(model, cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? $"Đã xuất báo cáo thành công (mã {result.Data?.ReferenceCode})."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("schedules")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSchedule(CreateReportScheduleViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng điền đầy đủ thông tin đặt lịch báo cáo.";
            return RedirectToAction(nameof(Index));
        }

        var result = await apiClient.CreateReportScheduleAsync(model, cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Đã đặt lịch báo cáo định kỳ thành công."
            : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("schedules/{id:int}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateSchedule(int id, CancellationToken cancellationToken)
    {
        var result = await apiClient.DeactivateReportScheduleAsync(id, cancellationToken);
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Đã tắt lịch báo cáo." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }
}
