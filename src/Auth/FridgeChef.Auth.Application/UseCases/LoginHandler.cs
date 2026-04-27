using FluentValidation;
using FridgeChef.Auth.Application.Dto;
using FridgeChef.Auth.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Auth.Application.UseCases;

public sealed record LoginRequest(string Email, string Password);

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email обязателен")
            .EmailAddress().WithMessage("Некорректный формат email");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Пароль обязателен");
    }
}

public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IRefreshTokenRepository refreshTokens)
{
    public async Task<Result<AuthTokensResponse>> HandleAsync(
        LoginRequest request, AuthClientContext ctx, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email, ct);
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            return DomainErrors.Auth.InvalidCredentials;

        if (user.IsBlocked)
            return DomainErrors.Auth.AccountBlocked;

        var now = DateTime.UtcNow;
        var updated = user with { LastLoginAt = now, UpdatedAt = now };
        await users.UpdateAsync(updated, ct);

        var accessToken = jwt.GenerateAccessToken(updated);
        var rawRefresh = jwt.GenerateRefreshToken();
        var rt = new RefreshToken(
            Guid.NewGuid(), user.Id,
            jwt.HashRefreshToken(rawRefresh),
            now.AddDays(30), null, ctx.UserAgent, ctx.Ip, now);
        await refreshTokens.AddAsync(rt, ct);

        return new AuthTokensResponse(accessToken, rawRefresh, rt.ExpiresAt);
    }
}
