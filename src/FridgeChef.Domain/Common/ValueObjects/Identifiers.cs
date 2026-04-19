namespace FridgeChef.Domain.Common;

/// <summary>
/// Strongly-typed recipe identifier.
/// </summary>
public readonly record struct RecipeId(Guid Value)
{
    public static RecipeId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed user identifier.
/// </summary>
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed food node identifier (bigint in DB).
/// </summary>
public readonly record struct FoodNodeId(long Value)
{
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed unit of measurement identifier.
/// </summary>
public readonly record struct UnitId(long Value)
{
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Strongly-typed taxon identifier (diet, cuisine, etc.).
/// </summary>
public readonly record struct TaxonId(long Value)
{
    public override string ToString() => Value.ToString();
}
