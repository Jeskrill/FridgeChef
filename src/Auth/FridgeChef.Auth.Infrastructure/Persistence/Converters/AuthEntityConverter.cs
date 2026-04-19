using FridgeChef.Auth.Domain;
using FridgeChef.Auth.Infrastructure.Persistence.Entities;

namespace FridgeChef.Auth.Infrastructure.Persistence.Converters;

internal static class AuthEntityConverter
{
    internal static User ToDomain(this UserEntity e) => new(
        Id: e.Id,
        Email: e.Email,
        PasswordHash: e.PasswordHash,
        DisplayName: e.DisplayName,
        AvatarUrl: e.AvatarUrl,
        Role: e.Role,
        IsBlocked: e.IsBlocked,
        LastLoginAt: e.LastLoginAt,
        CreatedAt: e.CreatedAt,
        UpdatedAt: e.UpdatedAt);

    internal static UserEntity ToEntity(this User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        PasswordHash = u.PasswordHash,
        DisplayName = u.DisplayName,
        AvatarUrl = u.AvatarUrl,
        Role = u.Role,
        IsBlocked = u.IsBlocked,
        LastLoginAt = u.LastLoginAt,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt
    };

    internal static RefreshToken ToDomain(this RefreshTokenEntity e) => new(
        Id: e.Id,
        UserId: e.UserId,
        TokenHash: e.TokenHash,
        ExpiresAt: e.ExpiresAt,
        RevokedAt: e.RevokedAt,
        UserAgent: e.UserAgent,
        Ip: e.Ip,
        CreatedAt: e.CreatedAt);

    internal static RefreshTokenEntity ToEntity(this RefreshToken t) => new()
    {
        Id = t.Id,
        UserId = t.UserId,
        TokenHash = t.TokenHash,
        ExpiresAt = t.ExpiresAt,
        RevokedAt = t.RevokedAt,
        UserAgent = t.UserAgent,
        Ip = t.Ip,
        CreatedAt = t.CreatedAt
    };
}
