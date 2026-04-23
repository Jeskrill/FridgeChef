namespace FridgeChef.Auth.Domain;

public enum UserRole
{
    User = 0,
    Admin = 1
}

public sealed record User(
    Guid Id,
    string Email,
    string PasswordHash,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    bool IsBlocked,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record RefreshToken(
    Guid Id,
    Guid UserId,
    string TokenHash,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    string? UserAgent,
    System.Net.IPAddress? Ip,
    DateTime CreatedAt)
{
    public bool IsRevoked => RevokedAt.HasValue;
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
}
