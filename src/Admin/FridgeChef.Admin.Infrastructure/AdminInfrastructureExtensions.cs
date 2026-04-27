using FridgeChef.Admin.Application.UseCases;
using FridgeChef.Auth.Application.UseCases;
using FridgeChef.Favorites.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;

namespace FridgeChef.Admin.Infrastructure;

internal sealed class AuthUserAdminReader(IUserRepository users) : IAdminUserReader
{
    public async Task<AdminUserListResponse> GetPagedAsync(
        string? query, int page, int pageSize, CancellationToken ct)
    {
        var (pagedUsers, totalCount) = await users.GetPagedAsync(query, page, pageSize, ct);
        var items = pagedUsers
            .Select(u => new AdminUserResponse(u.Id, u.DisplayName, u.Email, u.Role, u.IsBlocked, u.LastLoginAt, u.CreatedAt))
            .ToList();
        return new AdminUserListResponse(items, totalCount, page, pageSize);
    }

    public Task<int> CountAsync(CancellationToken ct) =>
        users.CountAsync(ct);
}

internal sealed class FavoritesAdminReader(IFavoriteRecipeRepository favorites) : IAdminFavoritesReader
{
    public Task<int> CountTotalAsync(CancellationToken ct) =>
        favorites.CountTotalAsync(ct);

    public Task<IReadOnlyList<(Guid RecipeId, int Count)>> GetMostFavoritedAsync(
        int limit, CancellationToken ct) =>
        favorites.GetMostFavoritedAsync(limit, ct);
}

public static class AdminInfrastructureExtensions
{
    public static IServiceCollection AddAdminInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAdminUserReader, AuthUserAdminReader>();
        services.AddScoped<IAdminFavoritesReader, FavoritesAdminReader>();
        return services;
    }
}
