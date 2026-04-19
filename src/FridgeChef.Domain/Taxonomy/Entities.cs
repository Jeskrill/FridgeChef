namespace FridgeChef.Domain.Taxonomy;

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

/// <summary>Mapped to taxonomy.taxons.</summary>
public sealed class Taxon
{
    public long Id { get; set; }
    public TaxonKind Kind { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
}

public interface ITaxonRepository
{
    Task<IReadOnlyList<Taxon>> GetByKindAsync(TaxonKind kind, CancellationToken ct = default);
    Task<IReadOnlyList<Taxon>> GetAllAsync(CancellationToken ct = default);
}
