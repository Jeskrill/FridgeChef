namespace FridgeChef.Pricing.Application;

public sealed record IngredientPriceResponse(
    long FoodNodeId,
    string ProductTitle,
    decimal Price,
    decimal? PromoPrice,
    string? ProductUrl,
    string RetailerName);

public interface IPricingRepository
{
    Task<IReadOnlyList<IngredientPriceResponse>> GetPricesForFoodNodesAsync(
        IEnumerable<long> foodNodeIds,
        CancellationToken ct = default);
}

public sealed class GetPricesHandler(IPricingRepository pricing)
{
    public async Task<IReadOnlyList<IngredientPriceResponse>> HandleAsync(
        long[] foodNodeIds, CancellationToken ct = default)
    {
        var distinctIds = foodNodeIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (distinctIds.Length == 0)
            return Array.Empty<IngredientPriceResponse>();

        return await pricing.GetPricesForFoodNodesAsync(distinctIds, ct);
    }
}
