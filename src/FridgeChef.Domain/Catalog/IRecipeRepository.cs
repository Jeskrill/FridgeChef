using FridgeChef.Domain.Common;

namespace FridgeChef.Domain.Catalog;

public interface IRecipeRepository
{
    Task<Recipe?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Batch-load recipes by a set of IDs. Order is not guaranteed.
    /// Use this instead of calling GetByIdAsync in a loop.
    /// </summary>
    Task<IReadOnlyList<Recipe>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    Task<PagedResult<Recipe>> GetCatalogAsync(
        string? query,
        long[]? dietIds,
        long[]? cuisineIds,
        PagedRequest paging,
        CancellationToken ct = default);

    /// <summary>
    /// Get recipes that contain any of the given food node IDs as ingredients.
    /// Used by the matching engine.
    /// </summary>
    Task<IReadOnlyList<Recipe>> GetByFoodNodeIdsAsync(
        IReadOnlySet<long> foodNodeIds,
        long[]? excludeAllergenNodeIds,
        long[]? dietFilterTaxonIds,
        int limit,
        CancellationToken ct = default);

    Task UpdateStatusAsync(Guid id, RecipeStatus status, CancellationToken ct = default);
}
