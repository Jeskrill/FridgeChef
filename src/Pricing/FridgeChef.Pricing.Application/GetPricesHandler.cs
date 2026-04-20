using FridgeChef.Pricing.Domain;

namespace FridgeChef.Pricing.Application;

// Ответ API с ценой на ингредиент из базы.
public sealed record IngredientPriceResponse(
    long FoodNodeId,
    string ProductTitle,
    decimal Price,
    decimal? PromoPrice,
    string? ProductUrl,
    string RetailerName);

// Загружает актуальные цены на ингредиенты из базы данных.
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

        var prices = await pricing.GetPricesForFoodNodesAsync(distinctIds, ct);
        return prices.Select(p => new IngredientPriceResponse(
            p.FoodNodeId, p.ProductTitle, p.Price, p.PromoPrice,
            p.ProductUrl, p.RetailerName)).ToList();
    }
}
