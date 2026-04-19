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

public interface IPantryRepository
{
    Task<IReadOnlyList<PantryItem>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<PantryItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid userId, long foodNodeId, CancellationToken ct = default);
    Task AddAsync(PantryItem item, CancellationToken ct = default);
    Task UpdateAsync(PantryItem item, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlySet<long>> GetFoodNodeIdsByUserAsync(Guid userId, CancellationToken ct = default);
}
