using FridgeChef.Application.Auth.Dto;
using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Common;
using FluentValidation;

namespace FridgeChef.Application.Auth.Login;

public sealed record LoginRequest(string Email, string Password);

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен");
    }
}

public sealed class LoginHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IAuthTransactionManager _transactions;

    public LoginHandler(
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
        LoginRequest request,
        AuthClientContext clientContext,
        CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);

        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash))
            return DomainErrors.Auth.InvalidCredentials;

        if (user.IsBlocked)
            return DomainErrors.Auth.AccountBlocked;

        return await _transactions.ExecuteAsync(async innerCt =>
        {
            var now = DateTime.UtcNow;
            user.LastLoginAt = now;
            user.UpdatedAt = now;
            await _users.UpdateAsync(user, innerCt);

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
