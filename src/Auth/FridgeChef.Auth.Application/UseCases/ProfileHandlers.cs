using FridgeChef.Auth.Domain;
using FridgeChef.SharedKernel;
using FluentValidation;

namespace FridgeChef.Auth.Application.UseCases;

public sealed record UserProfileResponse(
    Guid Id, string DisplayName, string Email,
    string? AvatarUrl, string Role, DateTime CreatedAt);

public sealed record UpdateProfileRequest(string? DisplayName, string? Email);
public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);

public interface IUserRepository
{

    Task<UserProfileResponse?> GetProfileByIdAsync(Guid id, CancellationToken ct = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeAsync(Guid tokenId, CancellationToken ct = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default);
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
    public async Task<Result<UserProfileResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await users.GetProfileByIdAsync(userId, ct);
        if (profile is null) return DomainErrors.NotFound.User(userId);
        return profile;
    }
}

public sealed class UpdateProfileHandler(IUserRepository users)
{
    public async Task<Result<UserProfileResponse>> HandleAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);

        if (request.Email is not null && await users.EmailExistsAsync(request.Email, ct))
            return DomainErrors.Auth.EmailAlreadyExists;

        var updated = user with {
            DisplayName = request.DisplayName ?? user.DisplayName,
            Email = request.Email ?? user.Email,
            UpdatedAt = DateTime.UtcNow
        };
        await users.UpdateAsync(updated, ct);

        return new UserProfileResponse(updated.Id, updated.DisplayName, updated.Email, updated.AvatarUrl, updated.Role, updated.CreatedAt);
    }
}

public sealed class ChangePasswordHandler(IUserRepository users, IPasswordHasher hasher)
{
    public async Task<Result> HandleAsync(
        Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);
        if (!hasher.Verify(request.OldPassword, user.PasswordHash))
            return DomainErrors.Auth.InvalidCredentials;

        var updated = user with {
            PasswordHash = hasher.Hash(request.NewPassword),
            UpdatedAt = DateTime.UtcNow
        };
        await users.UpdateAsync(updated, ct);
        return Result.Success();
    }
}
