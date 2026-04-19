namespace FridgeChef.Taxonomy.Domain;

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

public interface ITaxonRepository
{
    Task<IReadOnlyList<Taxon>> GetByKindAsync(TaxonKind kind, CancellationToken ct = default);
    Task<IReadOnlyList<Taxon>> GetAllAsync(CancellationToken ct = default);
}
