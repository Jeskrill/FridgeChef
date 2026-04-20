using FridgeChef.Domain.Pricing;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Infrastructure.Persistence.Pricing;

internal sealed class PricingRepository : IPricingRepository
{
    private readonly FridgeChefDbContext _db;
    public PricingRepository(FridgeChefDbContext db) => _db = db;

    public async Task<IReadOnlyList<IngredientPrice>> GetPricesForFoodNodesAsync(
        IEnumerable<long> foodNodeIds, CancellationToken ct = default)
    {
        var ids = foodNodeIds.ToList();
        if (ids.Count == 0) return Array.Empty<IngredientPrice>();

        var result = await _db.Database
            .SqlQueryRaw<IngredientPrice>(
                """
                SELECT DISTINCT ON (ipm.food_node_id)
                    ipm.food_node_id AS "FoodNodeId",
                    rp.title AS "ProductTitle",
                    ps.price AS "Price",
                    ps.promo_price AS "PromoPrice",
                    rp.url AS "ProductUrl",
                    r.name AS "RetailerName"
                FROM pricing.ingredient_product_matches ipm
                JOIN pricing.retailer_products rp ON rp.id = ipm.retailer_product_id
                JOIN pricing.retailers r ON r.id = rp.retailer_id
                JOIN LATERAL (
                    SELECT price, promo_price
                    FROM pricing.price_snapshots
                    WHERE retailer_product_id = rp.id
                    ORDER BY captured_at DESC
                    LIMIT 1
                ) ps ON true
                WHERE ipm.food_node_id = ANY({0})
                  AND rp.is_active = true
                  AND r.is_active = true
                ORDER BY ipm.food_node_id, ipm.is_primary DESC, ipm.score DESC
                """,
                ids.ToArray())
            .ToListAsync(ct);

        return result;
    }
}
