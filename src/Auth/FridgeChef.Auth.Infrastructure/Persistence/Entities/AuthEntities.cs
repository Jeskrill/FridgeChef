namespace FridgeChef.Auth.Infrastructure.Persistence.Entities;

internal sealed class UserEntity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "user";
    public bool IsBlocked { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<RefreshTokenEntity> RefreshTokens { get; set; } = [];
}

internal sealed class RefreshTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? UserAgent { get; set; }
    public System.Net.IPAddress? Ip { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserEntity User { get; set; } = null!;
}
