using FridgeChef.Pricing.Domain;

namespace FridgeChef.Pricing.Application;

public interface IPricingRepository
{
    Task<IReadOnlyList<IngredientPrice>> GetPricesForFoodNodesAsync(
        IEnumerable<long> foodNodeIds,
        CancellationToken ct);
}

public sealed class GetPricesHandler(IPricingRepository pricing)
{
    public async Task<IReadOnlyList<IngredientPrice>> HandleAsync(
        long[] foodNodeIds, CancellationToken ct)
    {
        var distinctIds = foodNodeIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (distinctIds.Length == 0)
            return Array.Empty<IngredientPrice>();

        return await pricing.GetPricesForFoodNodesAsync(distinctIds, ct);
    }
}
