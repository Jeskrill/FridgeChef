using FridgeChef.SharedKernel;

namespace FridgeChef.Auth.Application.UseCases;

public sealed class LogoutHandler(IRefreshTokenRepository refreshTokens)
{
    public async Task<Result> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        await refreshTokens.RevokeAllForUserAsync(userId, ct);
        return Result.Success();
    }
}
