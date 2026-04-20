using FridgeChef.Auth.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Admin.Application.UseCases;

public sealed record AdminUserResponse(
    Guid Id, string DisplayName, string Email,
    string Role, bool IsBlocked, DateTime? LastLoginAt, DateTime CreatedAt);
public sealed record AdminUserListResponse(
    IReadOnlyList<AdminUserResponse> Users, int TotalCount, int Page, int PageSize);
public sealed record SetUserBlockedRequest(bool IsBlocked);
public sealed record AdminStatsResponse(
    int TotalUsers, int TotalRecipes, int TotalFavorites,
    IReadOnlyList<AdminPopularRecipeResponse> PopularRecipes);
public sealed record AdminPopularRecipeResponse(
    Guid Id, string Slug, string Title, string? ImageUrl, int FavoriteCount);

// Контракт для получения расширенного списка пользователей (Admin-операции).
public interface IAdminUserReader
{
    Task<IReadOnlyList<User>> GetAllAsync(string? query, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}

// Контракт для статистики каталога.
public interface IAdminRecipeReader
{
    Task<int> CountAsync(CancellationToken ct = default);
    Task<AdminPopularRecipeResponse?> GetSummaryByIdAsync(Guid id, CancellationToken ct = default);
}

// Контракт для статистики избранного.
public interface IAdminFavoritesReader
{
    Task<int> CountTotalAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(Guid RecipeId, int Count)>> GetMostFavoritedAsync(int limit, CancellationToken ct = default);
}

public sealed class GetAdminUsersHandler(IAdminUserReader users)
{
    public async Task<AdminUserListResponse> HandleAsync(
        string? query, int page, int pageSize, CancellationToken ct = default)
    {
        var allUsers = await users.GetAllAsync(query, ct);
        var total    = allUsers.Count;
        var paged    = allUsers
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new AdminUserResponse(u.Id, u.DisplayName, u.Email, u.Role, u.IsBlocked, u.LastLoginAt, u.CreatedAt))
            .ToList();
        return new AdminUserListResponse(paged, total, page, pageSize);
    }
}

public sealed class SetUserBlockedHandler(IUserRepository users)
{
    public async Task<Result<AdminUserResponse>> HandleAsync(
        Guid userId, SetUserBlockedRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null) return DomainErrors.NotFound.User(userId);

        var updated = user with { IsBlocked = request.IsBlocked, UpdatedAt = DateTime.UtcNow };
        await users.UpdateAsync(updated, ct);
        return new AdminUserResponse(updated.Id, updated.DisplayName, updated.Email, updated.Role, updated.IsBlocked, updated.LastLoginAt, updated.CreatedAt);
    }
}

public sealed class GetAdminStatsHandler(
    IAdminUserReader users,
    IAdminRecipeReader recipes,
    IAdminFavoritesReader favorites)
{
    public async Task<AdminStatsResponse> HandleAsync(CancellationToken ct = default)
    {
        var totalUsers   = await users.CountAsync(ct);
        var totalRecipes = await recipes.CountAsync(ct);
        var totalFavs    = await favorites.CountTotalAsync(ct);
        var topRecipes   = await favorites.GetMostFavoritedAsync(5, ct);

        var popular = new List<AdminPopularRecipeResponse>();
        foreach (var (recipeId, count) in topRecipes)
        {
            var summary = await recipes.GetSummaryByIdAsync(recipeId, ct);
            if (summary is not null)
                popular.Add(summary with { FavoriteCount = count });
        }

        return new AdminStatsResponse(totalUsers, totalRecipes, totalFavs, popular);
    }
}
