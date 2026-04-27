using FluentValidation;
using FridgeChef.Auth.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Auth.Application.UseCases;

public sealed record UserProfileResponse(
    Guid Id, string DisplayName, string Email,
    string? AvatarUrl, string Role, DateTime CreatedAt);

public sealed record UpdateProfileRequest(string? DisplayName, string? Email);
public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);

public interface IUserRepository
{

    Task<UserProfileResponse?> GetProfileByIdAsync(Guid id, CancellationToken ct);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct);
    Task<(IReadOnlyList<User> Users, int TotalCount)> GetPagedAsync(string? query, int page, int pageSize, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task RevokeAsync(Guid tokenId, CancellationToken ct);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct);
}

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x).Must(x => x.DisplayName is not null || x.Email is not null)
            .WithMessage("Нужно передать хотя бы одно поле для обновления");
        When(x => x.DisplayName is not null, () =>
            RuleFor(x => x.DisplayName!).NotEmpty().MaximumLength(100));
        When(x => x.Email is not null, () =>
            RuleFor(x => x.Email!).NotEmpty().EmailAddress().WithMessage("Некорректный email"));
    }
}

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.OldPassword).NotEmpty().WithMessage("Текущий пароль обязателен");
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6).WithMessage("Минимум 6 символов");
    }
}

public sealed class GetProfileHandler(IUserRepository users)
{
    public async Task<Result<UserProfileResponse>> HandleAsync(Guid userId, CancellationToken ct)
    {
        var profile = await users.GetProfileByIdAsync(userId, ct);
        if (profile is null) return DomainErrors.NotFound.User(userId);
        return profile;
    }
}

public sealed class UpdateProfileHandler(IUserRepository users)
{
    public async Task<Result<UserProfileResponse>> HandleAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);

        var normalizedEmail = request.Email is not null
            ? NormalizeEmail(request.Email)
            : user.Email;

        if (request.Email is not null &&
            !string.Equals(normalizedEmail, user.Email, StringComparison.OrdinalIgnoreCase) &&
            await users.EmailExistsAsync(normalizedEmail, ct))
            return DomainErrors.Auth.EmailAlreadyExists;

        var updated = user with
        {
            DisplayName = request.DisplayName?.Trim() ?? user.DisplayName,
            Email = normalizedEmail,
            UpdatedAt = DateTime.UtcNow
        };
        await users.UpdateAsync(updated, ct);

        return new UserProfileResponse(updated.Id, updated.DisplayName, updated.Email, updated.AvatarUrl, updated.Role, updated.CreatedAt);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}

public sealed class ChangePasswordHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IRefreshTokenRepository refreshTokens)
{
    public async Task<Result> HandleAsync(
        Guid userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);
        if (!hasher.Verify(request.OldPassword, user.PasswordHash))
            return DomainErrors.Auth.WrongPassword;

        var updated = user with
        {
            PasswordHash = hasher.Hash(request.NewPassword),
            UpdatedAt = DateTime.UtcNow
        };
        await users.UpdateAsync(updated, ct);
        await refreshTokens.RevokeAllForUserAsync(userId, ct);
        return Result.Success();
    }
}
