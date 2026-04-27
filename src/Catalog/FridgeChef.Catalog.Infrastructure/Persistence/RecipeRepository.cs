using FridgeChef.Catalog.Application;
using FridgeChef.Catalog.Application.Converters;
using FridgeChef.Catalog.Application.Dto;
using FridgeChef.Catalog.Domain;
using FridgeChef.Catalog.Infrastructure.Persistence.Converters;
using FridgeChef.Catalog.Infrastructure.Persistence.Entities;
using FridgeChef.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Catalog.Infrastructure.Persistence;

internal sealed class RecipeRepository : IRecipeRepository
{
    private readonly CatalogDbContext _db;

    public RecipeRepository(CatalogDbContext db) => _db = db;

    public async Task<RecipeDetailResponse?> GetDetailBySlugAsync(string slug, CancellationToken ct)
    {
        var entity = await _db.Recipes
            .AsSplitQuery()
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.Sections)
            .Include(r => r.Media)
            .Include(r => r.Nutrition)
            .Include(r => r.Equipment)
            .Include(r => r.RecipeTaxons)
            .Include(r => r.Allergens)
            .FirstOrDefaultAsync(r => r.Slug == slug && r.Status == "published", ct);

        return entity?.ToDomain().ToDetailDto();
    }

    public async Task<PagedResult<RecipeCardResponse>> GetCatalogAsync(
        string? query,
        long[]? dietIds,
        long[]? cuisineIds,
        string? cuisineName,
        int? maxTimeMin,
        decimal? maxKcal,
        PagedRequest paging,
        CancellationToken ct)
    {
        var q = _db.Recipes.Where(r => r.Status == "published");

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = LikeHelper.ContainsPattern(query.Trim().ToLowerInvariant());
            q = q.Where(r => EF.Functions.ILike(r.Title, pattern, LikeHelper.EscapeCharacter));
        }

        if (dietIds is { Length: > 0 })
            q = q.Where(r => r.RecipeTaxons.Any(rt => dietIds.Contains(rt.TaxonId)));

        if (cuisineIds is { Length: > 0 })
            q = q.Where(r => r.RecipeTaxons.Any(rt => cuisineIds.Contains(rt.TaxonId)));

        if (!string.IsNullOrWhiteSpace(cuisineName))
        {
            var trimmedName = cuisineName.Trim();
            q = q.Where(r => r.RecipeTaxons.Any(rt =>
                _db.Database.SqlQueryRaw<long>(
                    "SELECT id AS \"Value\" FROM ontology.taxons WHERE kind = 'cuisine' AND name ILIKE {0}",
                    trimmedName)
                .Any(tid => tid == rt.TaxonId)));
        }

        if (maxTimeMin.HasValue)
            q = q.Where(r => r.TotalTimeMin != null && r.TotalTimeMin <= maxTimeMin.Value);

        if (maxKcal.HasValue)
            q = q.Where(r => r.Nutrition != null && r.Nutrition.KcalPerServing != null &&
                             r.Nutrition.KcalPerServing <= maxKcal.Value);

        var totalCount = await q.CountAsync(ct);

        var entities = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip(paging.Skip)
            .Take(paging.EffectivePageSize)
            .AsSplitQuery()
            .Include(r => r.Media)
            .Include(r => r.Nutrition)
            .Include(r => r.Ingredients)
            .ToListAsync(ct);

        var cards = entities.Select(e => e.ToDomain().ToCardDto()).ToList();
        return new PagedResult<RecipeCardResponse>(cards, totalCount, paging.EffectivePage, paging.EffectivePageSize);
    }

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _db.Recipes
            .AsSplitQuery()
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.Media)
            .Include(r => r.Nutrition)
            .Include(r => r.RecipeTaxons)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return entity?.ToDomain();
    }

    public async Task<IReadOnlyList<Recipe>> GetByFoodNodeIdsAsync(
        IReadOnlySet<long> foodNodeIds,
        long[]? excludeAllergenNodeIds,
        long[]? dietFilterTaxonIds,
        int limit,
        CancellationToken ct)
    {
        var idsArray = foodNodeIds.ToArray();

        var q = _db.Recipes
            .Where(r => r.Status == "published")
            .Where(r => r.Ingredients.Any(i => i.FoodNodeId != null && idsArray.Contains(i.FoodNodeId.Value)));

        if (excludeAllergenNodeIds is { Length: > 0 })
            q = q.Where(r => !r.Allergens.Any(a => excludeAllergenNodeIds.Contains(a.AllergenNodeId)));

        if (dietFilterTaxonIds is { Length: > 0 })
            q = q.Where(r => r.RecipeTaxons.Any(rt => dietFilterTaxonIds.Contains(rt.TaxonId)));

        var matchingIds = await q
            .Take(limit)
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (matchingIds.Count == 0) return [];

        var entities = await _db.Recipes
            .Where(r => matchingIds.Contains(r.Id))
            .AsSplitQuery()
            .Include(r => r.Ingredients)
            .Include(r => r.Media)
            .Include(r => r.Nutrition)
            .Include(r => r.Allergens)
            .ToListAsync(ct);

        return entities.Select(e => e.ToDomain()).ToList();
    }

    public Task<int> CountAsync(CancellationToken ct) =>
        _db.Recipes.CountAsync(ct);

    public async Task<IReadOnlyList<RecipeSummary>> GetSummariesByIdsAsync(
        IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var result = await _db.Recipes
            .Where(r => ids.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                r.Slug,
                r.Title,
                ImageUrl = r.Media
                    .Where(m => m.MediaKind == "hero")
                    .Select(m => m.Url)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);
        return result
            .Select(r => new RecipeSummary(r.Id, r.Slug, r.Title, r.ImageUrl))
            .ToList();
    }

    public async Task UpdateStatusAsync(Guid id, RecipeStatus status, CancellationToken ct)
    {
        await _db.Recipes
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, status.ToString().ToLowerInvariant())
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow), ct);
    }
}
