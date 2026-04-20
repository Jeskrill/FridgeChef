using FridgeChef.Application.Auth.Dto;
using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Common;
using FluentValidation;

namespace FridgeChef.Application.Auth.RefreshToken;

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token обязателен")
            .MinimumLength(32).WithMessage("Refresh token слишком короткий");
    }
}

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
        RefreshTokenRequest request,
        AuthClientContext clientContext,
        CancellationToken ct = default)
    {
        var tokenHash = _jwt.HashRefreshToken(request.RefreshToken);
        var existing = await _refreshTokens.GetByTokenHashAsync(tokenHash, ct);

        if (existing is null || existing.ExpiresAt < DateTime.UtcNow)
            return DomainErrors.Auth.InvalidRefreshToken;

        // Generate new pair
        var accessToken = _jwt.GenerateAccessToken(existing.User);
        var newRefreshValue = _jwt.GenerateRefreshToken();

        var newRefreshToken = new Domain.Auth.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = _jwt.HashRefreshToken(newRefreshValue),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            UserAgent = clientContext.UserAgent,
            Ip = clientContext.Ip
        };

        var rotated = await _refreshTokens.RotateAsync(existing.Id, newRefreshToken, ct);
        if (!rotated)
            return DomainErrors.Auth.InvalidRefreshToken;

        return new AuthTokensResponse(accessToken, newRefreshValue, newRefreshToken.ExpiresAt);
    }
}
