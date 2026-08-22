using CloneEbay.Application.Sellers;
using Microsoft.AspNetCore.Authorization;

namespace CloneEbay.API.Authorization;

public sealed class VerifiedSellerRequirement : IAuthorizationRequirement;

public sealed class VerifiedSellerAuthorizationHandler(ISellerService sellerService)
    : AuthorizationHandler<VerifiedSellerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        VerifiedSellerRequirement requirement)
    {
        var userIdValue = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdValue, out var userId) && await sellerService.IsVerifiedAsync(userId))
        {
            context.Succeed(requirement);
        }
    }
}
