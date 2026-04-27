using FridgeChef.SharedKernel;

namespace FridgeChef.Favorites.Application.UseCases;

public sealed record RecipeSummaryDto(Guid Id, string Slug, string Title, string? ImageUrl);

public interface IRecipeSummaryProvider
{
    Task<IReadOnlyList<RecipeSummaryDto>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
}

public sealed record FavoriteRecipeResponse(
    Guid RecipeId, string Slug, string Title, string? ImageUrl, DateTime AddedAt);

public interface IFavoriteRecipeRepository
{
    Task<IReadOnlyList<FavoriteRecipeResponse>> GetByUserIdAsync(Guid userId, IRecipeSummaryProvider recipes, CancellationToken ct);
    Task<bool> ExistsAsync(Guid userId, Guid recipeId, CancellationToken ct);
    Task AddAsync(Guid userId, Guid recipeId, CancellationToken ct);
    Task RemoveAsync(Guid userId, Guid recipeId, CancellationToken ct);
    Task<int> CountTotalAsync(CancellationToken ct);
    Task<IReadOnlyList<(Guid RecipeId, int Count)>> GetMostFavoritedAsync(int limit, CancellationToken ct);
}

public sealed class GetFavoritesHandler(
    IFavoriteRecipeRepository favorites,
    IRecipeSummaryProvider recipes)
{
    public Task<IReadOnlyList<FavoriteRecipeResponse>> HandleAsync(
        Guid userId, CancellationToken ct)
        => favorites.GetByUserIdAsync(userId, recipes, ct);
}

public sealed class AddFavoriteHandler(
    IFavoriteRecipeRepository favorites,
    IRecipeSummaryProvider recipes)
{
    public async Task<Result> HandleAsync(Guid userId, Guid recipeId, CancellationToken ct)
    {
        var recipeSummaries = await recipes.GetByIdsAsync([recipeId], ct);
        if (recipeSummaries.Count == 0)
            return DomainErrors.NotFound.Recipe(recipeId);

        if (await favorites.ExistsAsync(userId, recipeId, ct))
            return Result.Success();

        await favorites.AddAsync(userId, recipeId, ct);
        return Result.Success();
    }
}

public sealed class RemoveFavoriteHandler(IFavoriteRecipeRepository favorites)
{
    public async Task<Result> HandleAsync(Guid userId, Guid recipeId, CancellationToken ct)
    {
        await favorites.RemoveAsync(userId, recipeId, ct);
        return Result.Success();
    }
}
