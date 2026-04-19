using FridgeChef.Catalog.Domain;
using FridgeChef.Catalog.Infrastructure.Persistence.Converters;
using FridgeChef.Catalog.Infrastructure.Persistence.Entities;
using FridgeChef.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Catalog.Infrastructure.Persistence;

/// <summary>
/// Implements IRecipeRepository using EF Core + CatalogDbContext.
/// Converts internal entities to domain records before returning.
/// The Application layer never sees EF entities.
/// </summary>
internal sealed class RecipeRepository : IRecipeRepository
{
    private readonly CatalogDbContext _db;

    public RecipeRepository(CatalogDbContext db) => _db = db;

    public async Task<Recipe?> GetBySlugAsync(string slug, CancellationToken ct = default)
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

        return entity?.ToDomain();
    }

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default)
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

    public async Task<PagedResult<Recipe>> GetCatalogAsync(
        string? query,
        long[]? dietIds,
        long[]? cuisineIds,
        PagedRequest paging,
        CancellationToken ct = default)
    {
        var q = _db.Recipes.Where(r => r.Status == "published");

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            q = q.Where(r => EF.Functions.ILike(r.Title, $"%{normalized}%"));
        }

        if (dietIds is { Length: > 0 })
            q = q.Where(r => r.RecipeTaxons.Any(rt => dietIds.Contains(rt.TaxonId)));

        if (cuisineIds is { Length: > 0 })
            q = q.Where(r => r.RecipeTaxons.Any(rt => cuisineIds.Contains(rt.TaxonId)));

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

        var items = entities.Select(e => e.ToDomain()).ToList();
        return new PagedResult<Recipe>(items, totalCount, paging.EffectivePage, paging.EffectivePageSize);
    }

    public async Task<IReadOnlyList<Recipe>> GetByFoodNodeIdsAsync(
        IReadOnlySet<long> foodNodeIds,
        long[]? excludeAllergenNodeIds,
        long[]? dietFilterTaxonIds,
        int limit,
        CancellationToken ct = default)
    {
        var idsArray = foodNodeIds.ToArray();

        var q = _db.Recipes
            .Where(r => r.Status == "published")
            .Where(r => r.Ingredients.Any(i => i.FoodNodeId != null && idsArray.Contains(i.FoodNodeId.Value)));

        if (excludeAllergenNodeIds is { Length: > 0 })
            q = q.Where(r => !r.Allergens.Any(a => excludeAllergenNodeIds.Contains(a.AllergenNodeId)));

        if (dietFilterTaxonIds is { Length: > 0 })
            q = q.Where(r => r.RecipeTaxons.Any(rt => dietFilterTaxonIds.Contains(rt.TaxonId)));

        // Step 1: get matching IDs (fast, no JOINs)
        var matchingIds = await q
            .Take(limit)
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (matchingIds.Count == 0) return [];

        // Step 2: load full graph for those IDs
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

    public async Task UpdateStatusAsync(Guid id, RecipeStatus status, CancellationToken ct = default)
    {
        await _db.Recipes
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, status.ToString().ToLowerInvariant())
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow), ct);
    }
}
