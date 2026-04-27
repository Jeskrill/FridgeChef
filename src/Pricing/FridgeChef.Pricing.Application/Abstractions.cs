using FridgeChef.Pricing.Domain;

namespace FridgeChef.Pricing.Application;

public sealed record ScrapedProductDto(
    string ExternalSku,
    string Title,
    string? Brand,
    decimal RegularPrice,
    decimal? DiscountPrice,
    string ProductUrl);

public interface IRetailerScraper
{

    string RetailerCode { get; }

    Task<IReadOnlyList<ScrapedProductDto>> SearchAsync(string query, CancellationToken ct);
}

public interface IBatchRetailerScraper : IRetailerScraper
{

    Task<Dictionary<string, IReadOnlyList<ScrapedProductDto>>> SearchBatchAsync(
        IReadOnlyList<string> queries, CancellationToken ct);
}

public interface IPriceSyncRepository
{
    Task<long> EnsureRetailerAsync(string code, string name, string baseUrl, CancellationToken ct);

    Task<long> UpsertRetailerProductAsync(
        long retailerId, string externalSku, string title, string? brand,
        string url, CancellationToken ct);

    Task InsertPriceSnapshotAsync(
        long retailerProductId, decimal price, decimal? promoPrice,
        CancellationToken ct);

    Task UpsertIngredientProductMatchAsync(
        long foodNodeId, long retailerProductId, CancellationToken ct);

    Task PersistBestMatchAsync(
        long retailerId,
        IngredientToScrape ingredient,
        ScrapedProductDto best,
        CancellationToken ct);

    Task<IReadOnlyList<IngredientToScrape>> GetActiveIngredientsAsync(CancellationToken ct);

    Task<PricingStatsResponse> GetStatsAsync(CancellationToken ct);
}

public sealed record IngredientToScrape(long FoodNodeId, string CanonicalName);

public sealed record PricingStatsResponse(
    int UpdatedProductsCount,
    int MissingPricesCount,
    DateTime? LastSyncAt);
