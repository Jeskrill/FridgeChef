using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Common;
using FluentValidation;

namespace FridgeChef.Application.Profile;

public sealed record UserProfileResponse(
    Guid Id,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    string Role,
    DateTime CreatedAt);

public sealed record UpdateProfileRequest(string? DisplayName, string? Email);
public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x)
            .Must(x => x.DisplayName is not null || x.Email is not null)
            .WithMessage("Нужно передать хотя бы одно поле для обновления");

        When(x => x.DisplayName is not null, () =>
        {
            RuleFor(x => x.DisplayName!)
                .NotEmpty().WithMessage("Имя не может быть пустым")
                .MaximumLength(100);
        });

        When(x => x.Email is not null, () =>
        {
            RuleFor(x => x.Email!)
                .NotEmpty().WithMessage("Email не может быть пустым")
                .EmailAddress().WithMessage("Некорректный формат email");
        });
    }
}

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("Текущий пароль обязателен");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Новый пароль обязателен")
            .MinimumLength(8).WithMessage("Новый пароль должен быть не менее 8 символов")
            .NotEqual(x => x.OldPassword).WithMessage("Новый пароль должен отличаться от текущего");
    }
}

public sealed class GetProfileHandler
{
    private readonly IUserRepository _users;
    public GetProfileHandler(IUserRepository users) => _users = users;

    public async Task<Result<UserProfileResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);

        return new UserProfileResponse(
            user.Id, user.DisplayName, user.Email,
            user.AvatarUrl, user.Role, user.CreatedAt);
    }
}

public sealed class UpdateProfileHandler
{
    private readonly IUserRepository _users;
    public UpdateProfileHandler(IUserRepository users) => _users = users;

    public async Task<Result<UserProfileResponse>> HandleAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);

        if (request.DisplayName is not null) user.DisplayName = request.DisplayName.Trim();

        if (request.Email is not null)
        {
            var newEmail = request.Email.Trim().ToLowerInvariant();
            if (newEmail != user.Email && await _users.EmailExistsAsync(newEmail, ct))
                return DomainErrors.Auth.EmailAlreadyTaken;
            user.Email = newEmail;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);

        return new UserProfileResponse(
            user.Id, user.DisplayName, user.Email,
            user.AvatarUrl, user.Role, user.CreatedAt);
    }
}

public sealed class ChangePasswordHandler
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IAuthTransactionManager _transactions;

    public ChangePasswordHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IRefreshTokenRepository refreshTokens,
        IAuthTransactionManager transactions)
    {
        _users = users;
        _hasher = hasher;
        _refreshTokens = refreshTokens;
        _transactions = transactions;
    }

    public async Task<Result> HandleAsync(
        Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);

        if (!_hasher.Verify(request.OldPassword, user.PasswordHash))
            return DomainErrors.Auth.WrongPassword;

        await _transactions.ExecuteAsync(async innerCt =>
        {
            user.PasswordHash = _hasher.Hash(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _users.UpdateAsync(user, innerCt);
            await _refreshTokens.RevokeAllForUserAsync(userId, innerCt);
        }, ct);

        return Result.Success();
    }
}
