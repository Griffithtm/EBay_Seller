using CloneEbay.Web.Models;
using CloneEbay.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.Web.Controllers;

[Route("payments")]
[Authorize(Policy = "VerifiedSeller")]
public sealed class PaymentsController(SellerApiClient apiClient) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string period = "thisMonth", string compared = "none", CancellationToken cancellationToken = default)
    {
        var result = await apiClient.GetPaymentsReportAsync(period, compared, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = result.ErrorMessage;
            return View(new PaymentsReportViewModel { Period = period, Compared = compared });
        }

        return View(new PaymentsReportViewModel
        {
            Period = period,
            Compared = compared,
            Report = result.Data
        });
    }
}
