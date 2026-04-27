using FluentValidation;
using FridgeChef.Auth.Application.Dto;
using FridgeChef.Auth.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Auth.Application.UseCases;

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh-токен не может быть пустым.");
    }
}

public sealed class RefreshTokenHandler(
    IUserRepository users,
    IJwtTokenService jwt,
    IRefreshTokenRepository refreshTokens)
{
    public async Task<Result<AuthTokensResponse>> HandleAsync(
        RefreshTokenRequest request, AuthClientContext ctx, CancellationToken ct)
    {
        var tokenHash = jwt.HashRefreshToken(request.RefreshToken);
        var stored = await refreshTokens.GetByTokenHashAsync(tokenHash, ct);

        if (stored is null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            return DomainErrors.Auth.InvalidRefreshToken;

        var user = await users.GetByIdAsync(stored.UserId, ct);
        if (user is null || user.IsBlocked)
            return DomainErrors.Auth.AccountBlocked;

        await refreshTokens.RevokeAsync(stored.Id, ct);

        var now = DateTime.UtcNow;
        var accessToken = jwt.GenerateAccessToken(user);
        var rawRefresh = jwt.GenerateRefreshToken();
        var rt = new RefreshToken(
            Guid.NewGuid(), user.Id,
            jwt.HashRefreshToken(rawRefresh),
            now.AddDays(30), null, ctx.UserAgent, ctx.Ip, now);
        await refreshTokens.AddAsync(rt, ct);

        return new AuthTokensResponse(accessToken, rawRefresh, rt.ExpiresAt);
    }
}
