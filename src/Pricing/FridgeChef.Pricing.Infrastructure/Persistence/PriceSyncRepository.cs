using FridgeChef.Pricing.Application;
using Microsoft.EntityFrameworkCore;

namespace FridgeChef.Pricing.Infrastructure.Persistence;

internal sealed class PriceSyncRepository : IPriceSyncRepository
{
    private readonly PricingDbContext _db;

    public PriceSyncRepository(PricingDbContext db) => _db = db;

    public async Task<long> EnsureRetailerAsync(
        string code, string name, string baseUrl, CancellationToken ct = default)
    {
        // RETURNING is non-composable in EF8 — use ToListAsync
        var ids = await _db.Database
            .SqlQueryRaw<long>(
                """
                INSERT INTO pricing.retailers (code, name, base_url, is_active)
                VALUES ({0}, {1}, {2}, true)
                ON CONFLICT (code) DO UPDATE SET name = EXCLUDED.name, base_url = EXCLUDED.base_url
                RETURNING id
                """,
                code, name, baseUrl)
            .ToListAsync(ct);

        return ids[0];
    }

    public async Task<long> UpsertRetailerProductAsync(
        long retailerId, string externalSku, string title, string? brand,
        string url, CancellationToken ct = default)
    {
        var ids = await _db.Database
            .SqlQueryRaw<long>(
                """
                INSERT INTO pricing.retailer_products (retailer_id, external_sku, title, brand, url, is_active)
                VALUES ({0}, {1}, {2}, {3}, {4}, true)
                ON CONFLICT (retailer_id, external_sku)
                DO UPDATE SET title = EXCLUDED.title, brand = EXCLUDED.brand,
                              url = EXCLUDED.url, is_active = true
                RETURNING id
                """,
                retailerId, externalSku, title, brand ?? (object)DBNull.Value, url)
            .ToListAsync(ct);

        return ids[0];
    }

    public async Task InsertPriceSnapshotAsync(
        long retailerProductId, decimal price, decimal? promoPrice,
        CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO pricing.price_snapshots (retailer_product_id, captured_at, price, promo_price, currency)
            VALUES ({0}, NOW(), {1}, {2}, 'RUB')
            """,
            [retailerProductId, price, promoPrice ?? (object)DBNull.Value],
            ct);
    }

    public async Task UpsertIngredientProductMatchAsync(
        long foodNodeId, long retailerProductId, CancellationToken ct = default)
    {
        await _db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO pricing.ingredient_product_matches
                (food_node_id, retailer_product_id, match_type, score, is_primary)
            VALUES ({0}, {1}, 'search_first', 1.0, true)
            ON CONFLICT (food_node_id, retailer_product_id)
            DO UPDATE SET match_type = 'search_first', score = 1.0, is_primary = true
            """,
            [foodNodeId, retailerProductId],
            ct);
    }

    public async Task PersistBestMatchAsync(
        long retailerId,
        IngredientToScrape ingredient,
        ScrapedProduct best,
        CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var productId = await UpsertRetailerProductAsync(
            retailerId,
            best.ExternalSku,
            best.Title,
            best.Brand,
            best.ProductUrl,
            ct);

        await InsertPriceSnapshotAsync(productId, best.RegularPrice, best.DiscountPrice, ct);

        await _db.Database.ExecuteSqlRawAsync(
            """
            UPDATE pricing.ingredient_product_matches
            SET is_primary = false
            WHERE food_node_id = {0}
              AND retailer_product_id <> {1}
              AND is_primary = true
            """,
            [ingredient.FoodNodeId, productId],
            ct);

        await UpsertIngredientProductMatchAsync(ingredient.FoodNodeId, productId, ct);

        await transaction.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<IngredientToScrape>> GetActiveIngredientsAsync(
        CancellationToken ct = default)
    {
        return await _db.Database
            .SqlQueryRaw<IngredientToScrape>(
                """
                SELECT id AS "FoodNodeId", canonical_name AS "CanonicalName"
                FROM ontology.food_nodes
                WHERE node_kind = 'ingredient' AND status = 'active'
                ORDER BY id
                """)
            .ToListAsync(ct);
    }
}
