namespace FridgeChef.Ontology.Infrastructure.Persistence.Entities;

internal sealed class FoodNodeEntity
{
    public long Id { get; set; }
    public string CanonicalName { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string NodeKind { get; set; } = null!;
    public string Status { get; set; } = null!;
    public long? MergedIntoId { get; set; }
    public long? DefaultUnitId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<FoodAliasEntity> Aliases { get; set; } = [];
}

internal sealed class FoodAliasEntity
{
    public long Id { get; set; }
    public long FoodNodeId { get; set; }
    public string AliasText { get; set; } = null!;
    public string AliasNormalized { get; set; } = null!;
    public string AliasType { get; set; } = null!;
    public string LanguageCode { get; set; } = "ru";
    public int Priority { get; set; } = 100;
    public bool IsPreferred { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public FoodNodeEntity FoodNode { get; set; } = null!;
}

internal sealed class FoodEdgeClosureEntity
{
    public long AncestorNodeId { get; set; }
    public long DescendantNodeId { get; set; }
    public string SemanticType { get; set; } = null!;
    public int Depth { get; set; }
}

internal sealed class UnitEntity
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public string QuantityClass { get; set; } = null!;
    public decimal ToBaseMultiplier { get; set; }
}
