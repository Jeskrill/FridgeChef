using FridgeChef.Auth.Domain;
using FridgeChef.Auth.Infrastructure.Persistence.Converters;
using FridgeChef.Auth.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Auth.Infrastructure.Persistence;

internal sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;
    public UserRepository(AuthDbContext db) => _db = db;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        return entity?.ToDomain();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var entity = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);
        return entity?.ToDomain();
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await _db.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        _db.Users.Add(user.ToEntity());
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        var entity = user.ToEntity();
        _db.Users.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _db;
    public RefreshTokenRepository(AuthDbContext db) => _db = db;

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var entity = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null, ct);
        return entity?.ToDomain();
    }

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Add(token.ToEntity());
        await _db.SaveChangesAsync(ct);
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
