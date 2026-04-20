using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace FridgeChef.Api;

/// <summary>
/// Helper to extract current user info from JWT claims.
/// </summary>
internal static class UserContext
{
    /// <summary>
    /// Returns the user ID from JWT claims, or null if the claim is missing or malformed.
    /// Use this in endpoints where you want to return 401 explicitly instead of throwing.
    /// </summary>
    public static Guid? TryGetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    /// <summary>
    /// Returns the user ID. Throws <see cref="InvalidOperationException"/> if the claim is absent —
    /// this should only be called inside endpoints already protected by RequireAuthorization()
    /// and with a valid JWT, so a missing sub indicates a configuration bug, not a client error.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        user.TryGetUserId()
        ?? throw new InvalidOperationException(
            "JWT sub claim is missing or not a valid Guid. Check token generation configuration.");

    public static bool IsAdmin(this ClaimsPrincipal user) =>
        user.Claims.Any(claim =>
            claim.Type == ClaimTypes.Role &&
            string.Equals(claim.Value, "admin", StringComparison.OrdinalIgnoreCase));
}
