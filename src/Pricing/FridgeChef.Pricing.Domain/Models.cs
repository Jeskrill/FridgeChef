namespace FridgeChef.Pricing.Domain;

public sealed record IngredientPrice(
    long FoodNodeId,
    string ProductTitle,
    decimal Price,
    decimal? PromoPrice,
    string? ProductUrl,
    string RetailerName);

public interface IPricingRepository
{
    Task<IReadOnlyList<IngredientPrice>> GetPricesForFoodNodesAsync(
        IEnumerable<long> foodNodeIds,
        CancellationToken ct = default);
}
