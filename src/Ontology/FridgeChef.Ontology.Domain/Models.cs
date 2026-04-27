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

public interface IFoodHierarchyRepository
{
    Task<IReadOnlySet<long>> ExpandDescendantsAsync(IEnumerable<long> foodNodeIds, CancellationToken ct);
    Task<IReadOnlySet<long>> GetAllergenFoodNodeIdsAsync(IEnumerable<long> allergenNodeIds, CancellationToken ct);
}

public enum TaxonKind
{
    Diet = 0,
    Cuisine = 1,
    DishType = 2,
    Occasion = 3,
    CookingMethod = 4,
    Feature = 5,
    SourceCollection = 6
}

public sealed record Taxon(
    long Id,
    TaxonKind Kind,
    string Name,
    string Slug,
    string? Description);
