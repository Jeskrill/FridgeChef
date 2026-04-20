using FridgeChef.Admin.Application.UseCases;
using FridgeChef.Auth.Domain;
using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Catalog.Domain;
using FridgeChef.Favorites.Application.UseCases;
using FridgeChef.Favorites.Domain;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Api.CrossBc;

// Адаптер, позволяющий Favorites.Application получать данные рецептов из Catalog.Infrastructure.
// Реализует IRecipeSummaryProvider по контракту Favorites BC без прямой зависимости на Catalog.Domain.
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

// Адаптер чтения пользователей для Admin BC.
internal sealed class AuthUserAdminReader(IUserRepository users) : IAdminUserReader
{
    public async Task<IReadOnlyList<User>> GetAllAsync(string? query, CancellationToken ct = default)
    {
        var all = await users.GetAllAsync(ct);
        if (string.IsNullOrWhiteSpace(query))
            return all;
        var q = query.Trim().ToLowerInvariant();
        return all.Where(u =>
            u.Email.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            u.DisplayName.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        var all = await users.GetAllAsync(ct);
        return all.Count;
    }
}

// Адаптер для статистики рецептов в Admin BC.
internal sealed class CatalogAdminRecipeReader(IRecipeRepository recipes) : IAdminRecipeReader
{
    public Task<int> CountAsync(CancellationToken ct = default) =>
        recipes.CountAsync(ct);

    public async Task<AdminPopularRecipeResponse?> GetSummaryByIdAsync(Guid id, CancellationToken ct = default)
    {
        var summaries = await recipes.GetSummariesByIdsAsync([id], ct);
        var r = summaries.FirstOrDefault();
        return r is null ? null : new AdminPopularRecipeResponse(r.Id, r.Slug, r.Title, r.ImageUrl, 0);
    }
}

// Адаптер для статистики избранного в Admin BC.
internal sealed class FavoritesAdminReader(IFavoriteRecipeRepository favorites) : IAdminFavoritesReader
{
    public Task<int> CountTotalAsync(CancellationToken ct = default) =>
        favorites.CountTotalAsync(ct);

    public Task<IReadOnlyList<(Guid RecipeId, int Count)>> GetMostFavoritedAsync(
        int limit, CancellationToken ct = default) =>
        favorites.GetMostFavoritedAsync(limit, ct);
}

// Регистрация всех кросс-BC адаптеров в DI.
public static class CrossBcAdapterExtensions
{
    public static IServiceCollection AddCrossBcAdapters(this IServiceCollection services)
    {
        services.AddScoped<IRecipeSummaryProvider, CatalogRecipeSummaryProvider>();
        services.AddScoped<IAdminUserReader, AuthUserAdminReader>();
        services.AddScoped<IAdminRecipeReader, CatalogAdminRecipeReader>();
        services.AddScoped<IAdminFavoritesReader, FavoritesAdminReader>();
        return services;
    }
}
