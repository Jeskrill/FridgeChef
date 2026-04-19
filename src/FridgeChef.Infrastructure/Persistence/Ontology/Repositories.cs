using FridgeChef.Domain.Ontology;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Infrastructure.Persistence.Ontology;

internal sealed class FoodNodeRepository : IFoodNodeRepository
{
    private readonly FridgeChefDbContext _db;
    public FoodNodeRepository(FridgeChefDbContext db) => _db = db;

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

    public async Task<FoodNode?> GetByIdAsync(long id, CancellationToken ct = default) =>
        await _db.FoodNodes
            .Include(n => n.Aliases)
            .FirstOrDefaultAsync(n => n.Id == id, ct);
}

internal sealed class UnitRepository : IUnitRepository
{
    private readonly FridgeChefDbContext _db;
    public UnitRepository(FridgeChefDbContext db) => _db = db;

    public async Task<IReadOnlyList<Unit>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Units.OrderBy(u => u.Name).ToListAsync(ct);
}

internal sealed class FoodHierarchyService : IFoodHierarchyService
{
    private readonly FridgeChefDbContext _db;
    public FoodHierarchyService(FridgeChefDbContext db) => _db = db;

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
