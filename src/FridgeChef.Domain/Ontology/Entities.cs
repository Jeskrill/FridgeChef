namespace FridgeChef.Domain.Ontology;

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

public enum EdgeType
{
    IsA = 0,
    PartOf = 1,
    Contains = 2,
    Triggers = 3,
    CanBeReplacedWith = 4,
    RelatedTo = 5
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

public enum SemanticType
{
    IsA = 0,
    Contains = 1,
    Allergen = 2
}

/// <summary>Mapped to ontology.food_nodes.</summary>
public sealed class FoodNode
{
    public long Id { get; set; }
    public string CanonicalName { get; set; } = null!;
    public string NormalizedName { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public FoodNodeKind NodeKind { get; set; }
    public FoodNodeStatus Status { get; set; }
    public long? MergedIntoId { get; set; }
    public long? DefaultUnitId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<FoodAlias> Aliases { get; set; } = new List<FoodAlias>();
}

/// <summary>Mapped to ontology.food_aliases.</summary>
public sealed class FoodAlias
{
    public long Id { get; set; }
    public long FoodNodeId { get; set; }
    public string AliasText { get; set; } = null!;
    public string AliasNormalized { get; set; } = null!;
    public AliasType AliasType { get; set; }
    public string LanguageCode { get; set; } = "ru";
    public int Priority { get; set; } = 100;
    public bool IsPreferred { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public FoodNode FoodNode { get; set; } = null!;
}

/// <summary>Mapped to ontology.food_edges.</summary>
public sealed class FoodEdge
{
    public long Id { get; set; }
    public long FromNodeId { get; set; }
    public long ToNodeId { get; set; }
    public string EdgeType { get; set; } = null!;
    public decimal Confidence { get; set; } = 1.0m;
    public string SourceKind { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Mapped to ontology.food_edge_closure (materialized DAG).</summary>
public sealed class FoodEdgeClosure
{
    public long AncestorNodeId { get; set; }
    public long DescendantNodeId { get; set; }
    public string SemanticType { get; set; } = null!;
    public int Depth { get; set; }
}

/// <summary>Mapped to ontology.units.</summary>
public sealed class Unit
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Symbol { get; set; } = null!;
    public string QuantityClass { get; set; } = null!;
    public decimal ToBaseMultiplier { get; set; }
}

/// <summary>Mapped to ontology.food_nutrient_profiles.</summary>
public sealed class FoodNutrientProfile
{
    public long Id { get; set; }
    public long FoodNodeId { get; set; }
    public string SourceName { get; set; } = null!;
    public string SourceRecordId { get; set; } = null!;
    public decimal? KcalPer100g { get; set; }
    public decimal? ProteinPer100g { get; set; }
    public decimal? FatPer100g { get; set; }
    public decimal? CarbsPer100g { get; set; }

    public FoodNode FoodNode { get; set; } = null!;
}
