namespace FridgeChef.Domain.Pantry;

public enum QuantityMode
{
    Exact = 0,
    PackageDefault = 1,
    CountOnly = 2,
    Unknown = 3
}

/// <summary>Mapped to user_domain.pantry_items.</summary>
public sealed class PantryItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public long FoodNodeId { get; set; }
    public decimal? QuantityValue { get; set; }
    public long? UnitId { get; set; }
    public QuantityMode QuantityMode { get; set; }
    public decimal? NormalizedAmountG { get; set; }
    public decimal? NormalizedAmountMl { get; set; }
    public string Source { get; set; } = "manual";
    public string? Note { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

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
