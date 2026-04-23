using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Catalog.Domain;
using FridgeChef.SharedKernel;

namespace FridgeChef.Catalog.Application;

public interface IRecipeRepository
{

    Task<PagedResult<RecipeCardResponse>> GetCatalogAsync(
        string? query,
        long[]? dietIds,
        long[]? cuisineIds,
        PagedRequest paging,
        CancellationToken ct = default);

    Task<RecipeDetailResponse?> GetDetailBySlugAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<Recipe>> GetByFoodNodeIdsAsync(
        IReadOnlySet<long> foodNodeIds,
        long[]? excludeAllergenNodeIds,
        long[]? dietFilterTaxonIds,
        int limit,
        CancellationToken ct = default);

    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task UpdateStatusAsync(Guid id, RecipeStatus status, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RecipeSummary>> GetSummariesByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);
}
