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
        string? cuisineName,
        int? maxTimeMin,
        decimal? maxKcal,
        PagedRequest paging,
        CancellationToken ct);

    Task<RecipeDetailResponse?> GetDetailBySlugAsync(string slug, CancellationToken ct);

    Task<IReadOnlyList<Recipe>> GetByFoodNodeIdsAsync(
        IReadOnlySet<long> foodNodeIds,
        long[]? excludeAllergenNodeIds,
        long[]? dietFilterTaxonIds,
        int limit,
        CancellationToken ct);

    Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct);

    Task UpdateStatusAsync(Guid id, RecipeStatus status, CancellationToken ct);
    Task<int> CountAsync(CancellationToken ct);
    Task<IReadOnlyList<RecipeSummary>> GetSummariesByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken ct);
}
