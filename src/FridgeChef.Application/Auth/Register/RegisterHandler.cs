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
    private readonly IAuthTransactionManager _transactions;

    public RegisterHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IRefreshTokenRepository refreshTokens,
        IAuthTransactionManager transactions)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _refreshTokens = refreshTokens;
        _transactions = transactions;
    }

    public async Task<Result<AuthTokensResponse>> HandleAsync(
        RegisterRequest request,
        AuthClientContext clientContext,
        CancellationToken ct = default)
    {
        if (await _users.EmailExistsAsync(request.Email, ct))
            return DomainErrors.Auth.EmailAlreadyTaken;

        return await _transactions.ExecuteAsync(async innerCt =>
        {
            var now = DateTime.UtcNow;

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email.Trim().ToLowerInvariant(),
                DisplayName = request.Name.Trim(),
                PasswordHash = _hasher.Hash(request.Password),
                Role = "user",
                LastLoginAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _users.AddAsync(user, innerCt);

            var accessToken = _jwt.GenerateAccessToken(user);
            var refreshTokenValue = _jwt.GenerateRefreshToken();

            var refreshToken = new Domain.Auth.RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = _jwt.HashRefreshToken(refreshTokenValue),
                ExpiresAt = now.AddDays(30),
                CreatedAt = now,
                UserAgent = clientContext.UserAgent,
                Ip = clientContext.Ip
            };

            await _refreshTokens.AddAsync(refreshToken, innerCt);

            return new AuthTokensResponse(accessToken, refreshTokenValue, refreshToken.ExpiresAt);
        }, ct);
    }
}
