using FridgeChef.Domain.Catalog;
using FridgeChef.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Infrastructure.Persistence.Catalog;

internal sealed class RecipeRepository : IRecipeRepository
{
    private readonly FridgeChefDbContext _db;
    public RecipeRepository(FridgeChefDbContext db) => _db = db;

    public async Task<Recipe?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await _db.Recipes
            .AsSplitQuery()
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.Sections)
            .Include(r => r.Media)
            .Include(r => r.Nutrition)
            .Include(r => r.Equipment)
            .Include(r => r.RecipeTaxons)
            .Include(r => r.Allergens)
            .FirstOrDefaultAsync(r => r.Slug == slug && r.Status == RecipeStatus.Published, ct);

    public async Task<Recipe?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Recipes
            .AsSplitQuery()
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .Include(r => r.Media)
            .Include(r => r.Nutrition)
            .Include(r => r.RecipeTaxons)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Recipe>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return Array.Empty<Recipe>();

        return await _db.Recipes
            .Where(r => idList.Contains(r.Id))
            .AsSplitQuery()
            .Include(r => r.Ingredients)
            .Include(r => r.Media)
            .Include(r => r.Nutrition)
            .Include(r => r.RecipeTaxons)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<Recipe>> GetCatalogAsync(
        string? query,
        long[]? dietIds,
        long[]? cuisineIds,
        PagedRequest paging,
        CancellationToken ct = default)
    {
        var q = _db.Recipes
            .Where(r => r.Status == RecipeStatus.Published);

        // Text search
        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            q = q.Where(r => EF.Functions.ILike(r.Title, $"%{normalized}%"));
        }

        // Diet filter
        if (dietIds is { Length: > 0 })
        {
            q = q.Where(r => r.RecipeTaxons.Any(rt => dietIds.Contains(rt.TaxonId)));
        }

        // Cuisine filter
        if (cuisineIds is { Length: > 0 })
        {
            q = q.Where(r => r.RecipeTaxons.Any(rt => cuisineIds.Contains(rt.TaxonId)));
        }

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip(paging.Skip)
            .Take(paging.EffectivePageSize)
            .AsSplitQuery()
            .Include(r => r.Media)
            .Include(r => r.Nutrition)
            .Include(r => r.Ingredients)
            .ToListAsync(ct);

        return new PagedResult<Recipe>(items, totalCount, paging.EffectivePage, paging.EffectivePageSize);
    }

    public async Task<IReadOnlyList<Recipe>> GetByFoodNodeIdsAsync(
        IReadOnlySet<long> foodNodeIds,
        long[]? excludeAllergenNodeIds,
        long[]? dietFilterTaxonIds,
        int limit,
        CancellationToken ct = default)
    {
        // Convert to array so EF Core can translate Contains() to SQL ANY()
        var idsArray = foodNodeIds.ToArray();

        var q = _db.Recipes
            .Where(r => r.Status == RecipeStatus.Published)
            .Where(r => r.Ingredients.Any(i => i.FoodNodeId != null && idsArray.Contains(i.FoodNodeId.Value)));

        // Exclude recipes with user's allergens
        if (excludeAllergenNodeIds is { Length: > 0 })
        {
            q = q.Where(r => !r.Allergens.Any(a => excludeAllergenNodeIds.Contains(a.AllergenNodeId)));
        }

        // Filter by diet
        if (dietFilterTaxonIds is { Length: > 0 })
        {
            q = q.Where(r => r.RecipeTaxons.Any(rt => dietFilterTaxonIds.Contains(rt.TaxonId)));
        }

        // Step 1: get matching recipe IDs (fast, no JOINs)
        var matchingIds = await q
            .Take(limit)
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (matchingIds.Count == 0) return Array.Empty<Recipe>();

        // Step 2: load full graph for those IDs (split query works correctly with IN clause, no LIMIT subquery issue)
        return await _db.Recipes
            .Where(r => matchingIds.Contains(r.Id))
            .AsSplitQuery()
            .Include(r => r.Ingredients)
            .Include(r => r.Media)
            .Include(r => r.Nutrition)
            .Include(r => r.Allergens)
            .ToListAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, RecipeStatus status, CancellationToken ct = default)
    {
        await _db.Recipes
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, status)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow), ct);
    }
}
