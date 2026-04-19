using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Common;

namespace FridgeChef.Application.Auth.Logout;

public sealed class LogoutHandler
{
    private readonly IRefreshTokenRepository _refreshTokens;

    public LogoutHandler(IRefreshTokenRepository refreshTokens) => _refreshTokens = refreshTokens;

    public async Task<Result> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        await _refreshTokens.RevokeAllForUserAsync(userId, ct);
        return Result.Success();
    }
}
