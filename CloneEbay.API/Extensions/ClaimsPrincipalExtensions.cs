using System.Security.Claims;
using CloneEbay.Application.Common;

namespace CloneEbay.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetRequiredUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return int.TryParse(value, out var userId)
            ? userId
            : throw new AppProblemException(401, "USER_ID_CLAIM_MISSING", "Authenticated user identifier is missing.");
    }
}
