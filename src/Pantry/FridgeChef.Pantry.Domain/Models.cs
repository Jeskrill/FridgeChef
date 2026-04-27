namespace FridgeChef.Pantry.Domain;

public enum QuantityMode
{
    Unknown = 0,
    Exact = 1,
    PackageDefault = 2,
    CountOnly = 3
}

public sealed record PantryItem(
    Guid Id,
    Guid UserId,
    long FoodNodeId,
    decimal? Quantity,
    long? UnitId,
    QuantityMode QuantityMode,
    DateTime CreatedAt);
