using CloneEbay.API.Authorization;
using CloneEbay.API.Extensions;
using CloneEbay.API.Models;
using CloneEbay.Application.Returns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloneEbay.API.Controllers;

[ApiController]
[Route("api/policies")]
public class PoliciesController(IReturnService returnService) : ControllerBase
{
    [HttpGet("stores/{storeId:int}/return-policy")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiEnvelope<ReturnPolicyResponse>>> GetReturnPolicy(
        int storeId,
        CancellationToken cancellationToken)
    {
        var result = await returnService.GetStoreReturnPolicyAsync(storeId, cancellationToken);
        if (result is null)
            return NotFound(ApiEnvelope<object>.Failure("POLICY_NOT_FOUND", "No return policy configured for this store.", HttpContext.TraceIdentifier));

        return Ok(ApiEnvelope<ReturnPolicyResponse>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("stores/{storeId:int}/return-policy")]
    [Authorize(Policy = AuthorizationPolicies.VerifiedSeller)]
    public async Task<ActionResult<ApiEnvelope<ReturnPolicyResponse>>> CreateOrUpdateReturnPolicy(
        int storeId,
        [FromBody] CreateOrUpdateReturnPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await returnService.CreateOrUpdateReturnPolicyAsync(User.GetRequiredUserId(), storeId, request, cancellationToken);
        return Ok(ApiEnvelope<ReturnPolicyResponse>.Ok(result, HttpContext.TraceIdentifier));
    }
}
