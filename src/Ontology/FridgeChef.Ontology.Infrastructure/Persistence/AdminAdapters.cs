using FridgeChef.Admin.Application.UseCases;
using FridgeChef.Ontology.Infrastructure.Persistence.Configurations;
using FridgeChef.Ontology.Infrastructure.Persistence.Entities;
using FridgeChef.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Ontology.Infrastructure.Persistence;

internal sealed class AdminIngredientAdapter : IAdminIngredientReader, IAdminIngredientWriter
{
    private readonly OntologyDbContext _db;
    public AdminIngredientAdapter(OntologyDbContext db) => _db = db;

    public async Task<AdminIngredientListResponse> GetPagedAsync(
        string? query, int page, int pageSize, CancellationToken ct)
    {
        var q = _db.FoodNodes.Where(n => n.Status != "deprecated");

        if (!string.IsNullOrWhiteSpace(query))
        {
            var pattern = LikeHelper.ContainsPattern(query.Trim().ToLowerInvariant());
            q = q.Where(n => EF.Functions.ILike(n.CanonicalName, pattern, LikeHelper.EscapeCharacter));
        }

        var totalCount = await q.CountAsync(ct);

        var entities = await q
            .OrderBy(n => n.CanonicalName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var unitIds = entities.Where(e => e.DefaultUnitId.HasValue).Select(e => e.DefaultUnitId!.Value).Distinct().ToList();
        var units = unitIds.Count > 0
            ? await _db.Units.Where(u => unitIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct)
            : new Dictionary<long, string>();

        var items = entities.Select(e => new AdminIngredientResponse(
            e.Id, e.CanonicalName, e.Slug, e.NodeKind, e.Status,
            e.DefaultUnitId.HasValue && units.TryGetValue(e.DefaultUnitId.Value, out var un) ? un : null,
            e.DefaultUnitId, e.CreatedAt
        )).ToList();

        return new AdminIngredientListResponse(items, totalCount, page, pageSize);
    }

    public async Task<AdminIngredientResponse?> GetByIdAsync(long id, CancellationToken ct)
    {
        var e = await _db.FoodNodes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (e is null) return null;

        string? unitName = null;
        if (e.DefaultUnitId.HasValue)
        {
            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Id == e.DefaultUnitId.Value, ct);
            unitName = unit?.Name;
        }

        return new AdminIngredientResponse(e.Id, e.CanonicalName, e.Slug, e.NodeKind, e.Status, unitName, e.DefaultUnitId, e.CreatedAt);
    }

    public async Task<AdminIngredientResponse> CreateAsync(CreateIngredientRequest req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var slug = ToSlug(req.CanonicalName);
        var entity = new FoodNodeEntity
        {
            CanonicalName = req.CanonicalName.Trim(),
            NormalizedName = req.CanonicalName.Trim().ToLowerInvariant(),
            Slug = slug,
            NodeKind = req.Kind?.Trim().ToLowerInvariant() ?? "ingredient",
            Status = "active",
            DefaultUnitId = req.DefaultUnitId,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.FoodNodes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new AdminIngredientResponse(entity.Id, entity.CanonicalName, entity.Slug, entity.NodeKind, entity.Status, null, entity.DefaultUnitId, entity.CreatedAt);
    }

    public async Task<AdminIngredientResponse?> UpdateAsync(long id, UpdateIngredientRequest req, CancellationToken ct)
    {
        var entity = await _db.FoodNodes.FirstOrDefaultAsync(n => n.Id == id, ct);
        if (entity is null) return null;

        if (req.CanonicalName is not null)
        {
            entity.CanonicalName = req.CanonicalName.Trim();
            entity.NormalizedName = req.CanonicalName.Trim().ToLowerInvariant();
            entity.Slug = ToSlug(req.CanonicalName);
        }
        if (req.Kind is not null) entity.NodeKind = req.Kind.Trim().ToLowerInvariant();
        if (req.DefaultUnitId.HasValue) entity.DefaultUnitId = req.DefaultUnitId;
        entity.UpdatedAt = DateTime.UtcNow;

        _db.FoodNodes.Update(entity);
        await _db.SaveChangesAsync(ct);

        return new AdminIngredientResponse(entity.Id, entity.CanonicalName, entity.Slug, entity.NodeKind, entity.Status, null, entity.DefaultUnitId, entity.CreatedAt);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct)
    {

        var affected = await _db.FoodNodes
            .Where(n => n.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.Status, "deprecated")
                .SetProperty(n => n.UpdatedAt, DateTime.UtcNow), ct);
        return affected > 0;
    }

    private static string ToSlug(string name) =>
        name.Trim().ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("ё", "е");
}

internal sealed class AdminTaxonAdapter : IAdminTaxonReader, IAdminTaxonWriter
{
    private readonly OntologyDbContext _db;
    public AdminTaxonAdapter(OntologyDbContext db) => _db = db;

    public async Task<AdminTaxonListResponse> GetAllAsync(string? kind, CancellationToken ct)
    {
        var q = _db.Taxons.AsQueryable();
        if (!string.IsNullOrWhiteSpace(kind))
        {
            var kindNormalized = ToSnakeCase(kind.Trim());
            q = q.Where(t => t.Kind == kindNormalized);
        }

        var entities = await q.OrderBy(t => t.Name).ToListAsync(ct);

        var taxonIds = entities.Select(e => e.Id).ToList();
        var recipeCounts = taxonIds.Count > 0
            ? await _db.Database
                .SqlQueryRaw<TaxonRecipeCount>(
                    """
                    SELECT taxon_id AS "TaxonId", COUNT(*)::int AS "Count"
                    FROM catalog.recipe_taxons
                    WHERE taxon_id = ANY({0})
                    GROUP BY taxon_id
                    """,
                    taxonIds.ToArray())
                .ToDictionaryAsync(x => x.TaxonId, x => x.Count, ct)
            : new Dictionary<long, int>();

        var items = entities.Select(e => new AdminTaxonResponse(
            e.Id,
            FormatKind(e.Kind),
            e.Name, e.Slug, e.Description,
            recipeCounts.GetValueOrDefault(e.Id, 0)
        )).ToList();

        return new AdminTaxonListResponse(items, items.Count);
    }

    public async Task<AdminTaxonResponse?> GetByIdAsync(long id, CancellationToken ct)
    {
        var e = await _db.Taxons.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (e is null) return null;

        var recipeCount = await _db.Database
            .SqlQueryRaw<int>(
                """SELECT COUNT(*)::int AS "Value" FROM catalog.recipe_taxons WHERE taxon_id = {0}""", id)
            .FirstOrDefaultAsync(ct);

        return new AdminTaxonResponse(e.Id, FormatKind(e.Kind), e.Name, e.Slug, e.Description, recipeCount);
    }

    public async Task<AdminTaxonResponse> CreateAsync(CreateTaxonRequest req, CancellationToken ct)
    {
        var slug = req.Name.Trim().ToLowerInvariant().Replace(" ", "-");
        var entity = new TaxonEntity
        {
            Kind = ToSnakeCase(req.Kind.Trim()),
            Name = req.Name.Trim(),
            Slug = slug,
            Description = req.Description?.Trim()
        };
        _db.Taxons.Add(entity);
        await _db.SaveChangesAsync(ct);

        return new AdminTaxonResponse(entity.Id, FormatKind(entity.Kind), entity.Name, entity.Slug, entity.Description, 0);
    }

    public async Task<AdminTaxonResponse?> UpdateAsync(long id, UpdateTaxonRequest req, CancellationToken ct)
    {
        var entity = await _db.Taxons.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null) return null;

        if (req.Name is not null)
        {
            entity.Name = req.Name.Trim();
            entity.Slug = req.Name.Trim().ToLowerInvariant().Replace(" ", "-");
        }
        if (req.Description is not null) entity.Description = req.Description.Trim();

        _db.Taxons.Update(entity);
        await _db.SaveChangesAsync(ct);

        return new AdminTaxonResponse(entity.Id, FormatKind(entity.Kind), entity.Name, entity.Slug, entity.Description, 0);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct)
    {
        var affected = await _db.Taxons
            .Where(t => t.Id == id)
            .ExecuteDeleteAsync(ct);
        return affected > 0;
    }

    private static string FormatKind(string dbKind) =>
        dbKind.Replace("_", " ").Trim();

    private static string ToSnakeCase(string name) =>
        System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(name);
}

internal sealed class TaxonRecipeCount
{
    public long TaxonId { get; set; }
    public int Count { get; set; }
}
