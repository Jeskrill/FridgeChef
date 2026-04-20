using FridgeChef.Favorites.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Favorites.Application.UseCases;

// Контракт для получения сводки рецепта из Catalog BC без прямой зависимости на Catalog.Domain.
public sealed record RecipeSummaryDto(Guid Id, string Slug, string Title, string? ImageUrl);

public interface IRecipeSummaryProvider
{
    Task<IReadOnlyList<RecipeSummaryDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}

public sealed record FavoriteRecipeResponse(
    Guid RecipeId, string Slug, string Title, string? ImageUrl, DateTime AddedAt);

public sealed class GetFavoritesHandler(
    IFavoriteRecipeRepository favorites,
    IRecipeSummaryProvider recipes)
{
    public async Task<IReadOnlyList<FavoriteRecipeResponse>> HandleAsync(
        Guid userId, CancellationToken ct = default)
    {
        var favs = await favorites.GetByUserIdAsync(userId, ct);
        if (favs.Count == 0) return Array.Empty<FavoriteRecipeResponse>();

        var summaries = await recipes.GetByIdsAsync(favs.Select(f => f.RecipeId), ct);
        var byId = summaries.ToDictionary(r => r.Id);

        return favs
            .Where(f => byId.ContainsKey(f.RecipeId))
            .Select(f => new FavoriteRecipeResponse(
                f.RecipeId, byId[f.RecipeId].Slug,
                byId[f.RecipeId].Title, byId[f.RecipeId].ImageUrl, f.CreatedAt))
            .ToList();
    }
}

public sealed class AddFavoriteHandler(IFavoriteRecipeRepository favorites)
{
    public async Task<Result> HandleAsync(Guid userId, Guid recipeId, CancellationToken ct = default)
    {
        if (await favorites.ExistsAsync(userId, recipeId, ct))
            return Result.Success();

        await favorites.AddAsync(new FavoriteRecipe(userId, recipeId, DateTime.UtcNow), ct);
        return Result.Success();
    }
}

public sealed class RemoveFavoriteHandler(IFavoriteRecipeRepository favorites)
{
    public async Task<Result> HandleAsync(Guid userId, Guid recipeId, CancellationToken ct = default)
    {
        await favorites.RemoveAsync(userId, recipeId, ct);
        return Result.Success();
    }
}
