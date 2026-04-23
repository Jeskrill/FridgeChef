using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FridgeChef.Api;

internal static class UserContext
{

    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static Guid GetUserId(this ClaimsPrincipal user) =>
        user.TryGetUserId()
        ?? throw new InvalidOperationException(
            "JWT sub claim is missing or not a valid Guid. Check token generation configuration.");

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.Claims.Any(claim =>
            claim.Type == ClaimTypes.Role &&
            string.Equals(claim.Value, "admin", StringComparison.OrdinalIgnoreCase));
}
