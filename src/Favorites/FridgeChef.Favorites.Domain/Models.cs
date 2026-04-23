namespace FridgeChef.Favorites.Domain;

public sealed record FavoriteRecipe(
    Guid UserId,
    Guid RecipeId,
    DateTime CreatedAt);
