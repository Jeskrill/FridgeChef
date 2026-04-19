namespace FridgeChef.Domain.Auth;

public enum UserRole
{
    User = 0,
    Admin = 1
}

/// <summary>User entity mapped to auth.users.</summary>
public sealed class User
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
}

/// <summary>Refresh token entity mapped to auth.refresh_tokens.</summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? UserAgent { get; set; }
    public System.Net.IPAddress? Ip { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;

    public bool IsRevoked => RevokedAt.HasValue;
}
