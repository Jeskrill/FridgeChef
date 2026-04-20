namespace FridgeChef.Favorites.Domain;

public sealed record FavoriteRecipe(
    Guid UserId,
    Guid RecipeId,
    DateTime CreatedAt);

public interface IFavoriteRecipeRepository
{
    Task<IReadOnlyList<FavoriteRecipe>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, Guid recipeId, CancellationToken ct = default);
    Task AddAsync(FavoriteRecipe favorite, CancellationToken ct = default);
    Task RemoveAsync(Guid userId, Guid recipeId, CancellationToken ct = default);
    Task<int> CountTotalAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(Guid RecipeId, int Count)>> GetMostFavoritedAsync(int limit, CancellationToken ct = default);
}
