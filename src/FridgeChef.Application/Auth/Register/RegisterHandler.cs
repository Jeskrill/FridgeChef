using FridgeChef.Application.Auth.Dto;
using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Common;

namespace FridgeChef.Application.Auth.Register;

public sealed class RegisterHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenRepository _refreshTokens;

    public RegisterHandler(
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
        RegisterRequest request, CancellationToken ct = default)
    {
        if (await _users.EmailExistsAsync(request.Email, ct))
            return DomainErrors.Auth.EmailAlreadyTaken;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            DisplayName = request.Name.Trim(),
            PasswordHash = _hasher.Hash(request.Password),
            Role = "user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user, ct);

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
