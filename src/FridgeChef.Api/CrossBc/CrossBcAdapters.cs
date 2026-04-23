using FridgeChef.Admin.Application.UseCases;
using FridgeChef.Auth.Application.UseCases;
using FridgeChef.Auth.Domain;
using FridgeChef.Catalog.Application;
using FridgeChef.Catalog.Domain;
using FridgeChef.Favorites.Application.UseCases;

namespace FridgeChef.Api.CrossBc;

internal sealed class CatalogRecipeSummaryProvider(IRecipeRepository recipes) : IRecipeSummaryProvider
{
    public async Task<IReadOnlyList<RecipeSummaryDto>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return Array.Empty<RecipeSummaryDto>();

        var result = await recipes.GetSummariesByIdsAsync(idList, ct);
        return result
            .Select(r => new RecipeSummaryDto(r.Id, r.Slug, r.Title, r.ImageUrl))
            .ToList();
    }
}

internal sealed class AuthUserAdminReader(IUserRepository users) : IAdminUserReader
{
    public async Task<IReadOnlyList<AdminUserResponse>> GetAllAsync(string? query, CancellationToken ct = default)
    {
        var all = await users.GetAllAsync(ct);
        IEnumerable<User> filtered = all;
        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLowerInvariant();
            filtered = all.Where(u =>
                u.Email.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                u.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase));
        }
        return filtered
            .Select(u => new AdminUserResponse(u.Id, u.DisplayName, u.Email, u.Role, u.IsBlocked, u.LastLoginAt, u.CreatedAt))
            .ToList();
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        var all = await users.GetAllAsync(ct);
        return all.Count;
    }
}

internal sealed class FavoritesAdminReader(IFavoriteRecipeRepository favorites) : IAdminFavoritesReader
{
    public Task<int> CountTotalAsync(CancellationToken ct = default) =>
        favorites.CountTotalAsync(ct);

    public Task<IReadOnlyList<(Guid RecipeId, int Count)>> GetMostFavoritedAsync(
        int limit, CancellationToken ct = default) =>
        favorites.GetMostFavoritedAsync(limit, ct);
}

public static class CrossBcAdapterExtensions
{
    public static IServiceCollection AddCrossBcAdapters(this IServiceCollection services)
    {
        services.AddScoped<IRecipeSummaryProvider, CatalogRecipeSummaryProvider>();
        services.AddScoped<IAdminUserReader, AuthUserAdminReader>();
        services.AddScoped<IAdminFavoritesReader, FavoritesAdminReader>();
        return services;
    }
}
