using FridgeChef.Auth.Application.UseCases;
using FridgeChef.Auth.Domain;
using FridgeChef.Auth.Infrastructure.Persistence.Converters;
using FridgeChef.Auth.Infrastructure.Persistence.Entities;
using FridgeChef.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Auth.Infrastructure.Persistence;

internal sealed class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;
    public UserRepository(AuthDbContext db) => _db = db;

    public async Task<UserProfileResponse?> GetProfileByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (entity is null) return null;

        return new UserProfileResponse(
            entity.Id, entity.DisplayName, entity.Email,
            entity.AvatarUrl, entity.Role, entity.CreatedAt);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        return entity?.ToDomain();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = NormalizeEmail(email);
        var entity = await _db.Users
            .FirstOrDefaultAsync(u => EF.Functions.ILike(
                u.Email,
                LikeHelper.EscapeForLike(normalizedEmail),
                LikeHelper.EscapeCharacter), ct);
        return entity?.ToDomain();
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct) =>
        await _db.Users.AnyAsync(u => EF.Functions.ILike(
            u.Email,
            LikeHelper.EscapeForLike(NormalizeEmail(email)),
            LikeHelper.EscapeCharacter), ct);

    public async Task AddAsync(User user, CancellationToken ct)
    {
        _db.Users.Add(NormalizeUserEmail(user).ToEntity());
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        var entity = NormalizeUserEmail(user).ToEntity();
        _db.Users.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct)
    {
        var entities = await _db.Users.OrderByDescending(u => u.CreatedAt).ToListAsync(ct);
        return entities.Select(e => e.ToDomain()).ToList();
    }

    public async Task<int> CountAsync(CancellationToken ct) =>
        await _db.Users.CountAsync(ct);

    public async Task<(IReadOnlyList<User> Users, int TotalCount)> GetPagedAsync(
        string? query, int page, int pageSize, CancellationToken ct)
    {
        IQueryable<UserEntity> q = _db.Users;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = LikeHelper.ContainsPattern(query.Trim().ToLowerInvariant());
            q = q.Where(u =>
                EF.Functions.ILike(u.Email, pattern, LikeHelper.EscapeCharacter) ||
                EF.Functions.ILike(u.DisplayName, pattern, LikeHelper.EscapeCharacter));
        }

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (entities.Select(e => e.ToDomain()).ToList(), total);
    }

    private static User NormalizeUserEmail(User user) =>
        user with { Email = NormalizeEmail(user.Email) };

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();
}

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AuthDbContext _db;
    public RefreshTokenRepository(AuthDbContext db) => _db = db;

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct)
    {
        var entity = await _db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null, ct);
        return entity?.ToDomain();
    }

    public async Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        _db.RefreshTokens.Add(token.ToEntity());
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(Guid tokenId, CancellationToken ct)
    {
        await _db.RefreshTokens
            .Where(t => t.Id == tokenId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct)
    {
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, DateTime.UtcNow), ct);
    }
}
