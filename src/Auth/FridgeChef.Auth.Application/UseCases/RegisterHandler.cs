using FluentValidation;
using FridgeChef.Auth.Application.Dto;
using FridgeChef.Auth.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Auth.Application.UseCases;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Некорректный email");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Минимум 6 символов");
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
    }
}

public sealed class RegisterHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IRefreshTokenRepository refreshTokens)
{
    public async Task<Result<AuthTokensResponse>> HandleAsync(
        RegisterRequest request, AuthClientContext ctx, CancellationToken ct)
    {
        var normalizedEmail = NormalizeEmail(request.Email);

        if (await users.EmailExistsAsync(normalizedEmail, ct))
            return DomainErrors.Auth.EmailAlreadyExists;

        var now = DateTime.UtcNow;
        var user = new User(
            Guid.NewGuid(), normalizedEmail,
            hasher.Hash(request.Password),
            request.DisplayName.Trim(), null,
            "user", false, null, now, now);
        await users.AddAsync(user, ct);

        var accessToken = jwt.GenerateAccessToken(user);
        var rawRefresh = jwt.GenerateRefreshToken();
        var rt = new RefreshToken(
            Guid.NewGuid(), user.Id,
            jwt.HashRefreshToken(rawRefresh),
            now.AddDays(30), null, ctx.UserAgent, ctx.Ip, now);
        await refreshTokens.AddAsync(rt, ct);

        return new AuthTokensResponse(accessToken, rawRefresh, rt.ExpiresAt);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}
