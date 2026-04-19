namespace FridgeChef.Auth.Domain;

public enum UserRole
{
    User = 0,
    Admin = 1
}

// ────────────────────────────────────────────────────────────────────
//  Domain records
// ────────────────────────────────────────────────────────────────────

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

// ────────────────────────────────────────────────────────────────────
//  Repository & service interfaces
// ────────────────────────────────────────────────────────────────────

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
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
