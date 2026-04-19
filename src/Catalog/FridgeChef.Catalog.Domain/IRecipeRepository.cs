using FridgeChef.SharedKernel;

namespace FridgeChef.Catalog.Domain;

/// <summary>
/// Repository contract for Recipe — defined in Domain, implemented in Infrastructure.
/// Returns pure domain models, not DB entities.
/// </summary>
public interface IRecipeRepository
{
    Task<Recipe?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<Recipe>> GetCatalogAsync(
        string? query,
        long[]? dietIds,
        long[]? cuisineIds,
        PagedRequest paging,
        CancellationToken ct = default);

    Task<IReadOnlyList<Recipe>> GetByFoodNodeIdsAsync(
        IReadOnlySet<long> foodNodeIds,
        long[]? excludeAllergenNodeIds,
        long[]? dietFilterTaxonIds,
        int limit,
        CancellationToken ct = default);

    Task UpdateStatusAsync(Guid id, RecipeStatus status, CancellationToken ct = default);
}
