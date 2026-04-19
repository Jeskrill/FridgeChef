namespace FridgeChef.Domain.Favorites;

/// <summary>Mapped to user_domain.favorite_recipes.</summary>
public sealed class FavoriteRecipe
{
    public Guid UserId { get; set; }
    public Guid RecipeId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public interface IFavoriteRecipeRepository
{
    Task<IReadOnlyList<FavoriteRecipe>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, Guid recipeId, CancellationToken ct = default);
    Task AddAsync(FavoriteRecipe favorite, CancellationToken ct = default);
    Task RemoveAsync(Guid userId, Guid recipeId, CancellationToken ct = default);
}
