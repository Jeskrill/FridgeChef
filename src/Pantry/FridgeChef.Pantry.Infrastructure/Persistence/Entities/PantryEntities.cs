namespace FridgeChef.Pantry.Infrastructure.Persistence.Entities;

internal sealed class PantryItemEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public long FoodNodeId { get; set; }
    public decimal? QuantityValue { get; set; }
    public long? UnitId { get; set; }
    public string QuantityMode { get; set; } = "unknown";
    public decimal? NormalizedAmountG { get; set; }
    public decimal? NormalizedAmountMl { get; set; }
    public string Source { get; set; } = "manual";
    public string? Note { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
