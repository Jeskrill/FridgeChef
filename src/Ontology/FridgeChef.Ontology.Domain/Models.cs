namespace FridgeChef.Ontology.Domain;

public enum FoodNodeKind
{
    Ingredient = 0,
    IngredientGroup = 1,
    Allergen = 2,
    ProductType = 3
}

public enum FoodNodeStatus
{
    Active = 0,
    Review = 1,
    Deprecated = 2,
    Merged = 3
}

public enum AliasType
{
    Synonym = 0,
    Lemma = 1,
    Spelling = 2,
    SourceRaw = 3,
    Short = 4,
    Abbreviation = 5
}

// ────────────────────────────────────────────────────────────────────
//  Domain records — immutable, no EF dependencies
// ────────────────────────────────────────────────────────────────────

public sealed record FoodNode(
    long Id,
    string CanonicalName,
    string NormalizedName,
    string Slug,
    FoodNodeKind NodeKind,
    FoodNodeStatus Status,
    long? MergedIntoId,
    long? DefaultUnitId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<FoodAlias> Aliases);

public sealed record FoodAlias(
    long Id,
    long FoodNodeId,
    string AliasText,
    string AliasNormalized,
    AliasType AliasType,
    string LanguageCode,
    int Priority,
    bool IsPreferred);

public sealed record Unit(
    long Id,
    string Code,
    string Name,
    string Symbol,
    string QuantityClass,
    decimal ToBaseMultiplier);

public sealed record FoodNodeSearchResult(
    long Id,
    string CanonicalName,
    string? AliasText,
    double Similarity);

// ────────────────────────────────────────────────────────────────────
//  Repository interfaces — defined in Domain, implemented in Infrastructure
// ────────────────────────────────────────────────────────────────────

public interface IFoodNodeRepository
{
    Task<IReadOnlyList<FoodNodeSearchResult>> SearchAsync(string query, int limit = 10, CancellationToken ct = default);
    Task<FoodNode?> GetByIdAsync(long id, CancellationToken ct = default);
}

public interface IUnitRepository
{
    Task<IReadOnlyList<Unit>> GetAllAsync(CancellationToken ct = default);
}

public interface IFoodHierarchyRepository
{
    Task<IReadOnlySet<long>> ExpandDescendantsAsync(IEnumerable<long> foodNodeIds, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(IEnumerable<long> allergenNodeIds, CancellationToken ct = default);
}
