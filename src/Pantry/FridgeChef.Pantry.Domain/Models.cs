namespace FridgeChef.Pantry.Domain;

public enum QuantityMode
{
    Exact = 0,
    PackageDefault = 1,
    CountOnly = 2,
    Unknown = 3
}

public sealed record PantryItem(
    Guid Id,
    Guid UserId,
    long FoodNodeId,
    decimal? QuantityValue,
    long? UnitId,
    QuantityMode QuantityMode,
    decimal? NormalizedAmountG,
    decimal? NormalizedAmountMl,
    string Source,
    string? Note,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
