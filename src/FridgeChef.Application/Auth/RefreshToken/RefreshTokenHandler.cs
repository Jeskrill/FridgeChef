using FridgeChef.Application.Auth.Dto;
using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Common;

namespace FridgeChef.Application.Auth.RefreshToken;

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed class RefreshTokenHandler
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService _jwt;

    public RefreshTokenHandler(IRefreshTokenRepository refreshTokens, IJwtTokenService jwt)
    {
        _refreshTokens = refreshTokens;
        _jwt = jwt;
    }

    public async Task<Result<AuthTokensResponse>> HandleAsync(
        RefreshTokenRequest request, CancellationToken ct = default)
    {
        var tokenHash = _jwt.HashRefreshToken(request.RefreshToken);
        var existing = await _refreshTokens.GetByTokenHashAsync(tokenHash, ct);

        if (existing is null || existing.ExpiresAt < DateTime.UtcNow)
            return DomainErrors.Auth.InvalidRefreshToken;

        // Revoke old token
        await _refreshTokens.RevokeAsync(existing.Id, ct);

        // Generate new pair
        var accessToken = _jwt.GenerateAccessToken(existing.User);
        var newRefreshValue = _jwt.GenerateRefreshToken();

        var newRefreshToken = new Domain.Auth.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = _jwt.HashRefreshToken(newRefreshValue),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokens.AddAsync(newRefreshToken, ct);

        return new AuthTokensResponse(accessToken, newRefreshValue, newRefreshToken.ExpiresAt);
    }
}
