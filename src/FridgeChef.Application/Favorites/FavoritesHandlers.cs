using FridgeChef.Application.Recipes.Dto;
using FridgeChef.Application.Mapping;
using FridgeChef.Domain.Catalog;
using FridgeChef.Domain.Common;
using FridgeChef.Domain.Favorites;

namespace FridgeChef.Application.Favorites;

public sealed class GetFavoritesHandler
{
    private readonly IFavoriteRecipeRepository _favorites;
    private readonly IRecipeRepository _recipes;

    public GetFavoritesHandler(IFavoriteRecipeRepository favorites, IRecipeRepository recipes)
    {
        _favorites = favorites;
        _recipes = recipes;
    }

    public async Task<IReadOnlyList<RecipeCardResponse>> HandleAsync(Guid userId, CancellationToken ct = default)
    {
        var favs = await _favorites.GetByUserIdAsync(userId, ct);
        if (favs.Count == 0) return Array.Empty<RecipeCardResponse>();

        // Single batch query — no N+1
        var recipeIds = favs.Select(f => f.RecipeId);
        var recipes = await _recipes.GetByIdsAsync(recipeIds, ct);

        // Build lookup to preserve favorite order (sorted by CreatedAt desc from repository)
        var byId = recipes.ToDictionary(r => r.Id);

        return favs
            .Where(f => byId.ContainsKey(f.RecipeId))
            .Select(f => byId[f.RecipeId].ToCardResponse())
            .ToList();
    }
}

public sealed class AddFavoriteHandler
{
    private readonly IFavoriteRecipeRepository _favorites;
    public AddFavoriteHandler(IFavoriteRecipeRepository favorites) => _favorites = favorites;

    public async Task<Result> HandleAsync(Guid userId, Guid recipeId, CancellationToken ct = default)
    {
        if (await _favorites.ExistsAsync(userId, recipeId, ct))
            return Result.Success(); // Idempotent — PUT semantics

        await _favorites.AddAsync(new FavoriteRecipe
        {
            UserId = userId,
            RecipeId = recipeId,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return Result.Success();
    }
}

public sealed class RemoveFavoriteHandler
{
    private readonly IFavoriteRecipeRepository _favorites;
    public RemoveFavoriteHandler(IFavoriteRecipeRepository favorites) => _favorites = favorites;

    public async Task<Result> HandleAsync(Guid userId, Guid recipeId, CancellationToken ct = default)
    {
        await _favorites.RemoveAsync(userId, recipeId, ct);
        return Result.Success();
    }
}
