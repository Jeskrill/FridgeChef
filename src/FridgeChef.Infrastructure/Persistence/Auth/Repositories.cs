using FridgeChef.Domain.Auth;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Infrastructure.Persistence.Auth;

internal sealed class UserRepository : IUserRepository
{
    private readonly FridgeChefDbContext _db;
    public UserRepository(FridgeChefDbContext db) => _db = db;

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == NormalizeEmail(email), ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await _db.Users.AnyAsync(u => u.Email == NormalizeEmail(email), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }
}

internal sealed class AuthTransactionManager : IAuthTransactionManager
{
    private readonly FridgeChefDbContext _db;

    public AuthTransactionManager(FridgeChefDbContext db) => _db = db;

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var result = await operation(ct);
        await transaction.CommitAsync(ct);
        return result;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await operation(ct);
        await transaction.CommitAsync(ct);
    }
}

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly FridgeChefDbContext _db;
    public RefreshTokenRepository(FridgeChefDbContext db) => _db = db;

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> RotateAsync(Guid tokenId, RefreshToken newToken, CancellationToken ct)
    {
        var revokedCount = await _db.RefreshTokens
            .Where(t => t.Id == tokenId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);

        if (revokedCount != 1)
        {
            return false;
        }

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task RevokeAsync(Guid tokenId, CancellationToken ct = default)
    {
        await _db.RefreshTokens
            .Where(t => t.Id == tokenId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }
}
