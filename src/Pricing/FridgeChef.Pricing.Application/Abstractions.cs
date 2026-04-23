namespace FridgeChef.Pricing.Application;

public sealed record ScrapedProduct(
    string ExternalSku,
    string Title,
    string? Brand,
    decimal RegularPrice,
    decimal? DiscountPrice,
    string ProductUrl);

public interface IRetailerScraper
{

    string RetailerCode { get; }

    Task<IReadOnlyList<ScrapedProduct>> SearchAsync(string query, CancellationToken ct = default);
}

public interface IBatchRetailerScraper : IRetailerScraper
{

    Task<Dictionary<string, IReadOnlyList<ScrapedProduct>>> SearchBatchAsync(
        IReadOnlyList<string> queries, CancellationToken ct = default);
}

public interface IPriceSyncRepository
{
    Task<long> EnsureRetailerAsync(string code, string name, string baseUrl, CancellationToken ct = default);

    Task<long> UpsertRetailerProductAsync(
        long retailerId, string externalSku, string title, string? brand,
        string url, CancellationToken ct = default);

    Task InsertPriceSnapshotAsync(
        long retailerProductId, decimal price, decimal? promoPrice,
        CancellationToken ct = default);

    Task UpsertIngredientProductMatchAsync(
        long foodNodeId, long retailerProductId, CancellationToken ct = default);

    Task PersistBestMatchAsync(
        long retailerId,
        IngredientToScrape ingredient,
        ScrapedProduct best,
        CancellationToken ct);

    Task<IReadOnlyList<IngredientToScrape>> GetActiveIngredientsAsync(CancellationToken ct = default);

    Task<PricingStatsResponse> GetStatsAsync(CancellationToken ct = default);
}

public sealed record IngredientToScrape(long FoodNodeId, string CanonicalName);

public sealed record PricingStatsResponse(
    int UpdatedProductsCount,
    int MissingPricesCount,
    DateTime? LastSyncAt);
