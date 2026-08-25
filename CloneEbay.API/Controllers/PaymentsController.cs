using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

/// <summary>
/// Spec B note: the reference PaymentsController had no [Authorize] at all (relied on a
/// side-effect of failing to parse an anonymous user's id). Explicit [Authorize] here, as
/// the spec itself says to fix.
/// </summary>
[ApiController]
[Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
[Route("api/payments")]
public sealed class PaymentsController(IPaymentsReportService paymentsReportService) : ControllerBase
{
    [HttpGet("reports")]
    public async Task<ActionResult<ApiEnvelope<PaymentReportResponse>>> GetReport(
        [FromQuery] string period = PaymentReportPeriod.ThisMonth,
        [FromQuery] string compared = PaymentReportCompared.None,
        CancellationToken cancellationToken = default)
    {
        var result = await paymentsReportService.GetReportAsync(
            User.GetRequiredUserId(), new PaymentReportFilter(period, compared), cancellationToken);
        return Ok(ApiEnvelope<PaymentReportResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
