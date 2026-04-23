using FridgeChef.Admin.Application.UseCases;
using FridgeChef.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Catalog.Infrastructure.Persistence;

internal sealed class AdminRecipeAdapter : IAdminRecipeReader, IAdminRecipeWriter
{
    private readonly CatalogDbContext _db;
    public AdminRecipeAdapter(CatalogDbContext db) => _db = db;

    public async Task<AdminRecipeListResponse> GetPagedAsync(
        string? query, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.Recipes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim().ToLowerInvariant();
            q = q.Where(r => EF.Functions.ILike(r.Title, $"%{normalized}%"));
        }

        var totalCount = await q.CountAsync(ct);

        var entities = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.RecipeTaxons)
            .Include(r => r.Nutrition)
            .ToListAsync(ct);

        var recipes = entities.Select(e =>
        {

            var cuisineTaxonId = e.RecipeTaxons.FirstOrDefault()?.TaxonId;

            return new AdminRecipeResponse(
                e.Id, e.Slug, e.Title,
                null /* Cuisine resolved at API level if needed */,
                e.TotalTimeMin,
                e.Nutrition?.KcalPerServing,
                e.Status, e.CreatedAt);
        }).ToList();

        return new AdminRecipeListResponse(recipes, totalCount, page, pageSize);
    }

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await _db.Recipes.CountAsync(ct);

    public async Task<AdminPopularRecipeResponse?> GetSummaryByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Recipes
            .Include(r => r.Media)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (entity is null) return null;

        var imageUrl = entity.Media.FirstOrDefault(m => m.MediaKind == "hero")?.Url;
        return new AdminPopularRecipeResponse(entity.Id, entity.Slug, entity.Title, imageUrl, 0);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        var affected = await _db.Recipes
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, status)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow), ct);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {

        var affected = await _db.Recipes
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, "archived")
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow), ct);
        return affected > 0;
    }
}
