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
