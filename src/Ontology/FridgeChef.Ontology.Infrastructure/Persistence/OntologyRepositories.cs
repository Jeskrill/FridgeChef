using FridgeChef.Ontology.Domain;
using FridgeChef.Ontology.Infrastructure.Persistence.Configurations;
using FridgeChef.Ontology.Infrastructure.Persistence.Converters;
using FridgeChef.Ontology.Infrastructure.Persistence.Entities;
using FridgeChef.Taxonomy.Domain;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Ontology.Infrastructure.Persistence;

internal sealed class FoodNodeRepository : IFoodNodeRepository
{
    private readonly OntologyDbContext _db;
    public FoodNodeRepository(OntologyDbContext db) => _db = db;

    public async Task<IReadOnlyList<FoodNodeSearchResult>> SearchAsync(
        string query, int limit = 10, CancellationToken ct = default)
    {
        var normalized = query.Trim().ToLowerInvariant();

        var results = await _db.Database
            .SqlQueryRaw<FoodNodeSearchResult>(
                """
                SELECT DISTINCT ON (fn.id)
                    fn.id AS "Id",
                    fn.canonical_name AS "CanonicalName",
                    fa.alias_text AS "AliasText",
                    similarity(fa.alias_normalized, {0})::float8 AS "Similarity"
                FROM ontology.food_aliases fa
                JOIN ontology.food_nodes fn ON fn.id = fa.food_node_id
                WHERE fa.alias_normalized % {0}
                  AND fn.status = 'active'
                ORDER BY fn.id, similarity(fa.alias_normalized, {0}) DESC
                LIMIT {1}
                """,
                normalized, limit)
            .ToListAsync(ct);

        return results.OrderByDescending(r => r.Similarity).ToList();
    }

    public async Task<FoodNode?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.FoodNodes
            .Include(n => n.Aliases)
            .FirstOrDefaultAsync(n => n.Id == id, ct);
        return entity?.ToDomain();
    }
}

internal sealed class UnitRepository : IUnitRepository
{
    private readonly OntologyDbContext _db;
    public UnitRepository(OntologyDbContext db) => _db = db;

    public async Task<IReadOnlyList<Unit>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _db.Units.OrderBy(u => u.Name).ToListAsync(ct);
        return entities.Select(e => e.ToDomain()).ToList();
    }
}

// Handles food hierarchy expansion queries.
internal sealed class FoodHierarchyRepository : IFoodHierarchyRepository
{
    private readonly OntologyDbContext _db;
    public FoodHierarchyRepository(OntologyDbContext db) => _db = db;

    public async Task<IReadOnlySet<long>> ExpandDescendantsAsync(
        IEnumerable<long> foodNodeIds, CancellationToken ct = default)
    {
        var ids = foodNodeIds.ToList();
        if (ids.Count == 0) return new HashSet<long>();

        var descendants = await _db.FoodEdgeClosures
            .Where(c => ids.Contains(c.AncestorNodeId) && c.SemanticType == "IS_A")
            .Select(c => c.DescendantNodeId)
            .ToListAsync(ct);

        var result = new HashSet<long>(ids);
        result.UnionWith(descendants);
        return result;
    }

    public async Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(
        IEnumerable<long> allergenNodeIds, CancellationToken ct = default)
    {
        var ids = allergenNodeIds.ToList();
        if (ids.Count == 0) return new HashSet<long>();

        var triggeredNodes = await _db.FoodEdgeClosures
            .Where(c => ids.Contains(c.AncestorNodeId) &&
                        (c.SemanticType == "ALLERGEN" || c.SemanticType == "IS_A"))
            .Select(c => c.DescendantNodeId)
            .ToListAsync(ct);

        var result = new HashSet<long>(ids);
        result.UnionWith(triggeredNodes);
        return result;
    }
}

internal sealed class TaxonRepository : ITaxonRepository
{
    private readonly OntologyDbContext _db;
    public TaxonRepository(OntologyDbContext db) => _db = db;

    public async Task<IReadOnlyList<Taxon>> GetByKindAsync(TaxonKind kind, CancellationToken ct = default)
    {
        var kindStr = ToSnakeCase(kind.ToString());
        var entities = await _db.Taxons.Where(t => t.Kind == kindStr).ToListAsync(ct);
        return entities.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Taxon>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _db.Taxons.OrderBy(t => t.Name).ToListAsync(ct);
        return entities.Select(ToDomain).ToList();
    }

    private static Taxon ToDomain(TaxonEntity e) => new(
        Id: e.Id,
        Kind: Enum.TryParse<TaxonKind>(e.Kind.Replace("_", ""), ignoreCase: true, out var k) ? k : TaxonKind.Diet,
        Name: e.Name,
        Slug: e.Slug,
        Description: e.Description);

    // Converts PascalCase enum name to snake_case for DB lookup.
    private static string ToSnakeCase(string name) =>
        string.Concat(name.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
}
