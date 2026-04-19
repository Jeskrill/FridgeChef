using FridgeChef.Application.Auth.Dto;
using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Common;

namespace FridgeChef.Application.Auth.Login;

public sealed record LoginRequest(string Email, string Password);

public sealed class LoginHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenRepository _refreshTokens;

    public LoginHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IRefreshTokenRepository refreshTokens)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _refreshTokens = refreshTokens;
    }

    public async Task<Result<AuthTokensResponse>> HandleAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            return DomainErrors.Auth.InvalidCredentials;

        if (user.IsBlocked)
            return DomainErrors.Auth.AccountBlocked;

        var accessToken = _jwt.GenerateAccessToken(user);
        var refreshTokenValue = _jwt.GenerateRefreshToken();

        var refreshToken = new Domain.Auth.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = _jwt.HashRefreshToken(refreshTokenValue),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokens.AddAsync(refreshToken, ct);

        return new AuthTokensResponse(accessToken, refreshTokenValue, refreshToken.ExpiresAt);
    }
}
