namespace FridgeChef.Domain.Ontology;

public interface IFoodNodeRepository
{
    Task<IReadOnlyList<FoodNodeSearchResult>> SearchAsync(string query, int limit = 10, CancellationToken ct = default);
    Task<FoodNode?> GetByIdAsync(long id, CancellationToken ct = default);
}

public sealed record FoodNodeSearchResult(
    long Id,
    string CanonicalName,
    string? AliasText,
    double Similarity);

public interface IUnitRepository
{
    Task<IReadOnlyList<Unit>> GetAllAsync(CancellationToken ct = default);
}

public interface IFoodHierarchyService
{
    Task<IReadOnlySet<long>> ExpandDescendantsAsync(IEnumerable<long> foodNodeIds, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(IEnumerable<long> allergenNodeIds, CancellationToken ct = default);
}
