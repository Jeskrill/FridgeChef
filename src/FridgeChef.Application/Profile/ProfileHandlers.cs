using FridgeChef.Domain.Auth;
using FridgeChef.Domain.Common;

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

    public ChangePasswordHandler(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<Result> HandleAsync(
        Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);

        if (!_hasher.Verify(request.OldPassword, user.PasswordHash))
            return DomainErrors.Auth.WrongPassword;

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);

        return Result.Success();
    }
}
